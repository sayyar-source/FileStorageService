using Azure.Storage.Blobs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Storage;
public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(string connectionString, ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    // Uploads a file to Azure Blob Storage and returns its URL
    public async Task<string> UploadFileAsync(IFormFile file, string blobName)
    {
        // Get or create the "files" container (a storage bucket) in Azure
        var containerClient = _blobServiceClient.GetBlobContainerClient("files");
        await containerClient.CreateIfNotExistsAsync();

        // Set up a specific blob (file) in the container with the given name
        var blobClient = containerClient.GetBlobClient(blobName);

        // Open the file’s data stream and upload it to Azure, overwriting if it already exists
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, true);

        _logger.LogInformation("File uploaded to Azure Blob Storage: {BlobName}", blobName);
        return blobClient.Uri.ToString();
    }

    // Uploads a specific file version to Azure Blob Storage and returns its URL
    public async Task<string> UploadVersionAsync(FileVersion version)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("files");

        // Create a unique blob name using the file ID, version number, and original name
        var blobName = $"{version.FileEntryId}/{version.VersionNumber}/{version.Name}";
        var blobClient = containerClient.GetBlobClient(blobName);

        using (var httpClient = new HttpClient())
        using (var stream = await httpClient.GetStreamAsync(version.FilePath))
        {
            await blobClient.UploadAsync(stream, true); // Overwrite if exists
        }
        _logger.LogInformation("Version file uploaded to Azure Blob Storage: {BlobName}", blobName);
        return blobClient.Uri.ToString();
    }

    // Deletes a file from Azure Blob Storage using its path
    public async Task DeleteFileAsync(string path)
    {
        var uri = new Uri(path);
        var blobClient = _blobServiceClient.GetBlobContainerClient("files").GetBlobClient(Path.GetFileName(uri.LocalPath));
        await blobClient.DeleteIfExistsAsync();
        _logger.LogInformation("File deleted from Azure Blob Storage: {Path}", path);
    }
}