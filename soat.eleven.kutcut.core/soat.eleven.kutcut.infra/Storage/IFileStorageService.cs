namespace soat.eleven.kutcut.infra.Storage
{
    public interface IFileStorageService
    {
        Task<string> SaveVideoAsync(Guid userId, Guid videoId, string extension, Stream fileStream);
        Task<Stream?> GetThumbnailZipAsync(Guid userId, Guid videoId);
        Task DeleteVideoAsync(Guid userId, Guid videoId, string filename);
    }
}
