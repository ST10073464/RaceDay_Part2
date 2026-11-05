using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.Api.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _profileContainer;
    private readonly BlobContainerClient _bannerContainer;

    public BlobStorageService(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Azure Storage connection string is not configured.");

        string profileContainerName =
            configuration["AzureStorage:ProfilePicturesContainer"]
            ?? "profile-pictures";

        string bannerContainerName =
            configuration["AzureStorage:EventBannersContainer"]
            ?? "event-banners";

        _profileContainer = new BlobContainerClient(connectionString, profileContainerName);
        _bannerContainer = new BlobContainerClient(connectionString, bannerContainerName);
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file, int userId)
    {
        ValidateImage(file, "profile picture");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"users/{userId}/{Guid.NewGuid():N}{extension}";
        return await UploadImageAsync(_profileContainer, blobName, file);
    }

    public string GetProfilePictureReadUrl(string profilePictureUrl)
    {
        return GetReadUrl(profilePictureUrl, _profileContainer);
    }

    public string GetEventBannerReadUrl(string bannerImageUrl)
    {
        if (string.IsNullOrWhiteSpace(bannerImageUrl))
            return string.Empty;

        // Existing banners may have been uploaded to profile-pictures.
        // Select the container from the stored URL rather than assuming
        // all banners have already been moved to event-banners.
        return GetReadUrl(bannerImageUrl, _bannerContainer, _profileContainer);
    }

    public async Task<string> UploadEventBannerAsync(IFormFile file, int eventId)
    {
        ValidateImage(file, "event banner image");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"events/{eventId}/banner-{Guid.NewGuid():N}{extension}";
        return await UploadImageAsync(_bannerContainer, blobName, file);
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        var (container, blobName) = ResolveBlob(imageUrl, _profileContainer, _bannerContainer);
        BlobClient blobClient = container.GetBlobClient(blobName);
        return await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    private static async Task<string> UploadImageAsync(
        BlobContainerClient container, string blobName, IFormFile file)
    {
        BlobClient blobClient = container.GetBlobClient(blobName);
        BlobHttpHeaders headers = new() { ContentType = file.ContentType };
        await using Stream stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });
        return blobClient.Uri.ToString();
    }

    private static void ValidateImage(IFormFile file, string description)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException($"A {description} is required.", nameof(file));

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            throw new ArgumentException("Only JPG, JPEG, PNG and WEBP images are allowed.");

        const long maximumFileSize = 5 * 1024 * 1024;
        if (file.Length > maximumFileSize)
            throw new ArgumentException($"The {description} cannot exceed 5 MB.");
    }

    private static string GetReadUrl(string storedUrl, params BlobContainerClient[] containers)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
            return string.Empty;

        var (container, blobName) = ResolveBlob(storedUrl, containers);
        BlobClient blobClient = container.GetBlobClient(blobName);
        if (!blobClient.CanGenerateSasUri)
            throw new InvalidOperationException("A SAS URL cannot be generated for this blob.");

        Uri sasUri = blobClient.GenerateSasUri(
            BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));
        return sasUri.ToString();
    }

    private static (BlobContainerClient Container, string BlobName) ResolveBlob(
        string storedUrl, params BlobContainerClient[] containers)
    {
        if (!Uri.TryCreate(storedUrl, UriKind.Absolute, out Uri? storedUri)
            || storedUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("The stored image URL is invalid.");

        foreach (BlobContainerClient container in containers)
        {
            // Do not generate SAS tokens for URLs from another storage account.
            if (!string.Equals(storedUri.Host, container.Uri.Host,
                StringComparison.OrdinalIgnoreCase))
                continue;

            string containerPath = $"/{container.Name}/";
            if (!storedUri.AbsolutePath.StartsWith(containerPath,
                StringComparison.OrdinalIgnoreCase))
                continue;

            string blobName = Uri.UnescapeDataString(
                storedUri.AbsolutePath[containerPath.Length..]);
            if (!string.IsNullOrWhiteSpace(blobName))
                return (container, blobName);
        }

        throw new InvalidOperationException(
            "The stored image URL does not belong to a configured image container.");
    }
}
