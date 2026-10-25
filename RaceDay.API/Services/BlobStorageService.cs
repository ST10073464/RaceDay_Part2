using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        string connectionString =
            configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Azure Storage connection string is not configured.");

        string containerName =
            configuration["AzureStorage:ProfilePicturesContainer"]
            ?? "profile-pictures";

        _containerClient = new BlobContainerClient(
            connectionString,
            containerName);
    }

    public async Task<string> UploadProfilePictureAsync(
        IFormFile file,
        int userId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException(
                "A profile picture is required.",
                nameof(file));
        }

        string extension =
            Path.GetExtension(file.FileName).ToLowerInvariant();

        string[] allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "Only JPG, JPEG, PNG and WEBP images are allowed.");
        }

        const long maximumFileSize = 5 * 1024 * 1024;

        if (file.Length > maximumFileSize)
        {
            throw new ArgumentException(
                "The profile picture cannot exceed 5 MB.");
        }

        string blobName =
            $"users/{userId}/{Guid.NewGuid():N}{extension}";

        BlobClient blobClient =
            _containerClient.GetBlobClient(blobName);

        BlobHttpHeaders headers = new()
        {
            ContentType = file.ContentType
        };

        await using Stream stream =
            file.OpenReadStream();

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = headers
            });

        return blobClient.Uri.ToString();
    }
}