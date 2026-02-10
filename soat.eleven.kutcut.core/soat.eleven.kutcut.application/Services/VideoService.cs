using FluentResults;
using soat.eleven.kutcut.application.Dtos.Common;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.domain.Entities;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Storage;
using InfraStatusEnum = soat.eleven.kutcut.infra.Models.Enums.StatusEnum;

namespace soat.eleven.kutcut.application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IRepository<VideoModel> _repository;
        private readonly IFileStorageService _fileStorage;

        public VideoService(IRepository<VideoModel> repository, IFileStorageService fileStorage)
        {
            _repository = repository;
            _fileStorage = fileStorage;
        }

        public async Task<Result<VideoResult>> CreateAsync(CreateVideoInput input)
        {
            var extension = Path.GetExtension(input.FileName);

            var domainResult = Video.Create(input.Title, input.UserId, input.FileName);
            if (domainResult.IsFailed)
                return Result.Fail<VideoResult>(domainResult.Errors);

            var video = domainResult.Value;

            await _fileStorage.SaveVideoAsync(video.UserId, video.Id, extension!, input.FileStream);

            var model = new VideoModel
            {
                Id = video.Id,
                Title = video.Title,
                UserId = video.UserId,
                Filename = video.Filename,
                Status = (InfraStatusEnum)(int)video.Status,
                CreatedAt = video.CreatedAt,
                UpdatedAt = video.UpdatedAt
            };

            await _repository.AddAsync(model);

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<PagedResult<VideoResult>>> GetAllAsync(int pageNumber, int pageSize)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(pageNumber, pageSize);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = new PagedResult<VideoResult>(
                items.Select(MapToResult),
                totalCount,
                pageNumber,
                pageSize,
                totalPages);

            return Result.Ok(result);
        }

        public async Task<Result<VideoResult>> GetByIdAsync(Guid id)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<VideoResult>("Vídeo não encontrado.");

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<PagedResult<VideoResult>>> GetByStatusAsync(StatusEnum status, int pageNumber, int pageSize)
        {
            var infraStatus = (InfraStatusEnum)(int)status;

            var (items, totalCount) = await _repository.GetPagedAsync(
                pageNumber,
                pageSize,
                v => v.Status == infraStatus);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = new PagedResult<VideoResult>(
                items.Select(MapToResult),
                totalCount,
                pageNumber,
                pageSize,
                totalPages);

            return Result.Ok(result);
        }

        public async Task<Result<VideoResult>> UpdateTitleAsync(Guid id, string? newTitle)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<VideoResult>("Vídeo não encontrado.");

            var domainVideo = Video.Create(model.Title, model.UserId, model.Filename);
            if (domainVideo.IsFailed)
                return Result.Fail<VideoResult>(domainVideo.Errors);

            var updateResult = domainVideo.Value.UpdateTitle(newTitle);
            if (updateResult.IsFailed)
                return Result.Fail<VideoResult>(updateResult.Errors);

            model.Title = domainVideo.Value.Title;
            model.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(model);

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<Stream>> DownloadThumbnailsAsync(Guid id)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<Stream>("Vídeo não encontrado.");

            var stream = await _fileStorage.GetThumbnailZipAsync(model.UserId, model.Id);
            if (stream is null)
                return Result.Fail<Stream>("Arquivo de thumbnails não encontrado.");

            return Result.Ok(stream);
        }

        private static VideoResult MapToResult(VideoModel model)
        {
            var domainStatus = (StatusEnum)(int)(model.Status ?? InfraStatusEnum.Pendente);

            return new VideoResult(
                model.Id,
                model.Title ?? string.Empty,
                model.UserId,
                model.Filename,
                domainStatus,
                domainStatus.ToString(),
                model.CreatedAt ?? DateTime.MinValue,
                model.UpdatedAt);
        }
    }
}
