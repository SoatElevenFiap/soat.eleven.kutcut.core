using FluentResults;
using soat.eleven.kutcut.application.Dtos.Common;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.application.Interfaces
{
    public interface IVideoService
    {
        Task<Result<VideoResult>> CreateAsync(CreateVideoInput input);
        Task<Result<PagedResult<VideoResult>>> GetAllAsync(int pageNumber, int pageSize);
        Task<Result<VideoResult>> GetByIdAsync(Guid id);
        Task<Result<PagedResult<VideoResult>>> GetByStatusAsync(StatusEnum status, int pageNumber, int pageSize);
        Task<Result<VideoResult>> UpdateTitleAsync(Guid id, string? newTitle);
        Task<Result<Stream>> DownloadThumbnailsAsync(Guid id);
    }
}
