namespace soat.eleven.kutcut.infra.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;

        public LocalFileStorageService(string basePath)
        {
            _basePath = basePath;
        }

        public async Task<string> SaveVideoAsync(Guid userId, Guid videoId, string extension, Stream fileStream)
        {
            var directory = Path.Combine(_basePath, "Storage", userId.ToString(), "videos");
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, $"{videoId}{extension}");

            await using var fileTarget = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileTarget);

            return filePath;
        }

        public Task<Stream?> GetThumbnailZipAsync(Guid userId, Guid videoId)
        {
            var filePath = Path.Combine(_basePath, "Storage", userId.ToString(), "thumbnails", $"{videoId}.zip");

            if (!File.Exists(filePath))
                return Task.FromResult<Stream?>(null);

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream?>(stream);
        }

        public Task DeleteVideoAsync(Guid userId, Guid videoId, string filename)
        {
            var filePath = Path.Combine(_basePath, "Storage", userId.ToString(), "videos", filename);

            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }
    }
}
