using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using soat.eleven.kutcut.application.Dtos.Common;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Exceptions;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.domain.Entities;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.queues.MessagesDtos;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Storage;
using InfraStatusEnum = soat.eleven.kutcut.infra.Models.Enums.StatusEnum;

namespace soat.eleven.kutcut.application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IRepository<VideoModel> _repository;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContext _userContext;
        private readonly IMessageSender _messageSender;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly ILogger<VideoService> _logger;

        public VideoService(
            IRepository<VideoModel> repository,
            IFileStorageService fileStorage,
            IUserContext userContext,
            IMessageSender messageSender,
            IOptions<RabbitMQSettings> rabbitMQSettings,
            ILogger<VideoService> logger)
        {
            _repository = repository;
            _fileStorage = fileStorage;
            _userContext = userContext;
            _messageSender = messageSender;
            _rabbitMQSettings = rabbitMQSettings.Value;
            _logger = logger;
        }

        public async Task<Result<VideoResult>> CreateAsync(CreateVideoInput input)
        {
            EnsureAuthenticated();
            
            var userId = _userContext.UserId;
            var extension = Path.GetExtension(input.FileName);

            var domainResult = Video.Create(input.Title, userId, input.FileName);
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

            await PublishVideoUploadedMessageAsync(video);

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<PagedResult<VideoResult>>> GetAllAsync(int pageNumber, int pageSize)
        {
            EnsureAuthenticated();

            var userId = _userContext.UserId;

            var (items, totalCount) = await _repository.GetPagedAsync(
                pageNumber,
                pageSize,
                v => v.UserId == userId);

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
            EnsureAuthenticated();

            var model = await _repository.GetByIdAsync(id);
            if (model is null || model.UserId != _userContext.UserId)
                return Result.Fail<VideoResult>("Vídeo não encontrado.");

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<PagedResult<VideoResult>>> GetByStatusAsync(StatusEnum status, int pageNumber, int pageSize)
        {
            EnsureAuthenticated();

            var userId = _userContext.UserId;
            var infraStatus = (InfraStatusEnum)(int)status;

            var (items, totalCount) = await _repository.GetPagedAsync(
                pageNumber,
                pageSize,
                v => v.Status == infraStatus && v.UserId == userId);

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
            EnsureAuthenticated();

            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<VideoResult>("Vídeo não encontrado.");

            if (model.UserId != _userContext.UserId)
                throw new ForbiddenAccessException();

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

        public async Task<Result<VideoResult>> UpdateStatusAsync(Guid id, StatusEnum newStatus)
        {
            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<VideoResult>("Vídeo não encontrado.");

            var domainVideo = Video.Create(model.Title, model.UserId, model.Filename);
            if (domainVideo.IsFailed)
                return Result.Fail<VideoResult>(domainVideo.Errors);

            domainVideo.Value.SetStatus(newStatus);

            model.Status = (InfraStatusEnum)(int)newStatus;
            model.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(model);

            return Result.Ok(MapToResult(model));
        }

        public async Task<Result<Stream>> DownloadThumbnailsAsync(Guid id)
        {
            EnsureAuthenticated();

            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail<Stream>("Vídeo não encontrado.");

            if (model.UserId != _userContext.UserId)
                throw new ForbiddenAccessException();

            var stream = await _fileStorage.GetThumbnailZipAsync(model.UserId, model.Id);
            if (stream is null)
                return Result.Fail<Stream>("Arquivo de thumbnails não encontrado.");

            return Result.Ok(stream);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            EnsureAuthenticated();

            var model = await _repository.GetByIdAsync(id);
            if (model is null)
                return Result.Fail("Vídeo não encontrado.");

            if (model.UserId != _userContext.UserId)
                throw new ForbiddenAccessException();

            await _fileStorage.DeleteVideoAsync(model.UserId, model.Id, model.Filename);
            await _repository.DeleteAsync(model);

            _logger.LogInformation(
                "Vídeo excluído com sucesso. VideoId: {VideoId}, UserId: {UserId}",
                id, model.UserId);

            return Result.Ok();
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

        private async Task PublishVideoUploadedMessageAsync(Video video)
        {
            var extension = Path.GetExtension(video.Filename);
            var videoInternalName = $"{video.Id}{extension}";

            var message = new VideoUploadedMessage
            {
                UserId = video.UserId,
                Filename = videoInternalName,
                Title = video.Title,
                MessageId = video.Id,
                Status = (int)video.Status
            };

            try
            {
                await _messageSender.SendMessage(
                    _rabbitMQSettings.VideoUploadedQueueName,
                    message);

                _logger.LogInformation(
                    "Mensagem de vídeo criado publicada com sucesso. VideoId: {VideoId}, MessageId: {MessageId}",
                    video.Id, message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha ao publicar mensagem de vídeo criado. VideoId: {VideoId}",
                    video.Id);
            }
        }

        private void EnsureAuthenticated()
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Usuário não autenticado.");
        }
    }
}
