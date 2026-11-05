using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        string connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");
        string containerName = configuration["AzureStorage:ProfilePicturesContainer"]
            ?? "profile-pictures";
        _containerClient = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file, int userId)
    {
        ValidateImage(file, "profile picture");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"users/{userId}/{Guid.NewGuid():N}{extension}";
        return await UploadImageAsync(file, blobName);
    }

    public string GetProfilePictureReadUrl(string profilePictureUrl)
    {
        if (!TryGetOwnedBlobName(profilePictureUrl, out string blobName))
            return string.Empty;

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        if (!blobClient.CanGenerateSasUri)
            return string.Empty;

        return blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddMinutes(30)).ToString();
    }

    public string GetEventBannerReadUrl(string bannerImageUrl)
    {
        if (string.IsNullOrWhiteSpace(bannerImageUrl))
            return string.Empty;

        if (!Uri.TryCreate(bannerImageUrl, UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.UserInfo))
            return string.Empty;

        // Azure images must be signed; external HTTPS images can be displayed as-is.
        if (IsOurStorageHost(uri))
            return GetProfilePictureReadUrl(bannerImageUrl);

        return uri.ToString();
    }

    public async Task<string> UploadEventBannerAsync(IFormFile file, int eventId)
    {
        ValidateImage(file, "event banner image");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"events/{eventId}/banner-{Guid.NewGuid():N}{extension}";
        return await UploadImageAsync(file, blobName);
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        // External URLs must never be deleted through our storage account.
        if (!TryGetOwnedBlobName(imageUrl, out string blobName))
            return false;

        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        return await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots);
    }

    private bool IsOurStorageHost(Uri uri)
    {
        Uri containerUri = _containerClient.Uri;
        return uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, containerUri.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == containerUri.Port;
    }

    private bool TryGetOwnedBlobName(string? imageUrl, out string blobName)
    {
        blobName = string.Empty;
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri)
            || !IsOurStorageHost(uri)
            || !string.IsNullOrEmpty(uri.UserInfo))
            return false;

        string prefix = _containerClient.Uri.AbsolutePath.TrimEnd('/') + "/";
        if (!uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string candidate = Uri.UnescapeDataString(uri.AbsolutePath[prefix.Length..]);
        if (string.IsNullOrWhiteSpace(candidate)
            || candidate.StartsWith('/')
            || candidate.Contains('\\')
            || candidate.Split('/').Any(segment => segment == "." || segment == ".."))
            return false;

        blobName = candidate;
        return true;
    }

    private async Task<string> UploadImageAsync(IFormFile file, string blobName)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(blobName);
        var headers = new BlobHttpHeaders { ContentType = file.ContentType };
        await using Stream stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });
        return blobClient.Uri.ToString();
    }

    private static void ValidateImage(IFormFile file, string description)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException($"A {description} is required.", nameof(file));

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new ArgumentException("Only JPG, JPEG, PNG and WEBP images are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException($"The {description} cannot exceed 5 MB.");

        string contentType = file.ContentType.ToLowerInvariant();
        bool matchingType = extension switch
        {
            ".jpg" or ".jpeg" => contentType == "image/jpeg",
            ".png" => contentType == "image/png",
            ".webp" => contentType == "image/webp",
            _ => false
        };
        if (!matchingType)
            throw new ArgumentException("The image file type does not match its extension.");
    }
}
