using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using soat.eleven.kutcut.infra.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace soat.eleven.kutcut.infra.Storage
{
    [ExcludeFromCodeCoverage(Justification = "Requires live Azure Blob Storage — covered by integration tests")]
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureBlobStorageSettings _settings;

        public AzureBlobStorageService(IOptions<AzureBlobStorageSettings> settings)
        {
            _settings = settings.Value;
            _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
        }

        public async Task<string> SaveVideoAsync(Guid userId, Guid videoId, string extension, Stream fileStream)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = $"{userId}/videos/{videoId}{extension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = GetContentType(extension)
            });

            return blobClient.Uri.ToString();
        }

        public async Task<Stream?> GetThumbnailZipAsync(Guid userId, Guid videoId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            var blobName = $"{userId}/thumbnails/{videoId}.zip";
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                return null;

            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }

        public async Task DeleteVideoAsync(Guid userId, Guid videoId, string filename)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            var blobName = $"{userId}/videos/{filename}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",
                _ => "application/octet-stream"
            };
        }
    }
}
