using Microsoft.Extensions.Logging;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.application.Processors
{
    public class VideoNotificationProcessor : IVideoNotificationProcessor
    {
        private readonly ILogger<VideoNotificationProcessor> _logger;
        private readonly IUserSerivce _userService;
        private readonly IUserNotificaton _userNotification;
        private readonly IVideoMessageFactory _videoMessageFactory;

        public VideoNotificationProcessor(
            ILogger<VideoNotificationProcessor> logger,
            IUserSerivce userService,
            IUserNotificaton userNotification,
            IVideoMessageFactory videoMessageFactory)
        {
            _logger = logger;
            _userService = userService;
            _userNotification = userNotification;
            _videoMessageFactory = videoMessageFactory;
        }

        public async Task ProcessVideoNotificationAsync(VideoProcessingMessage videoMessage)
        {
            try
            {
                if (videoMessage == null)
                {
                    _logger.LogError("Failed to deserialize video processing message");
                    return;
                }

                var user = await GetUserAsync(videoMessage.UserId);
                if (user == null)
                {
                    _logger.LogError("User not found: {UserId}", videoMessage.UserId);
                    return;
                }

                var notifyMessage = _videoMessageFactory.BuildNotificationMessage(videoMessage);
                if (notifyMessage == null)
                {
                    _logger.LogWarning("Unknown video status: {Status}", videoMessage.Status);
                    return;
                }

                _userNotification.NotifyUser(user, notifyMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video notification message");
                throw;
            }
        }

        private async Task<UserDto?> GetUserAsync(Guid userId)
        {
            return await Task.FromResult(_userService.GetUser(userId));
        }

    }
}
