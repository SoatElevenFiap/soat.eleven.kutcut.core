using Microsoft.Extensions.Logging;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.queues.MessagesDtos;
using System.Text.Json;

namespace soat.eleven.kutcut.application.Processors
{
    public class VideoNotificationProcessor : IVideoNotificationProcessor
    {
        private readonly ILogger<VideoNotificationProcessor> _logger;
        private readonly IUserSerivce _userService;
        private readonly IUserNotificaton _userNotification;

        public VideoNotificationProcessor(
            ILogger<VideoNotificationProcessor> logger,
            IUserSerivce userService,
            IUserNotificaton userNotification)
        {
            _logger = logger;
            _userService = userService;
            _userNotification = userNotification;
        }

        public async Task ProcessVideoNotificationAsync(string message)
        {
            try
            {
                _logger.LogInformation("Processing video notification message: {Message}", message);

                var videoMessage = DeserializeMessage(message);
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

                var notifyMessage = BuildNotificationMessage(videoMessage);
                if (notifyMessage == null)
                {
                    _logger.LogWarning("Unknown video status: {Status}", videoMessage.Status);
                    return;
                }

                await SendNotificationAsync(user, notifyMessage, videoMessage.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video notification message");
                throw;
            }
        }

        private VideoProcessingMessage? DeserializeMessage(string message)
        {
            return JsonSerializer.Deserialize<VideoProcessingMessage>(message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<UserDto?> GetUserAsync(Guid userId)
        {
            return await Task.FromResult(_userService.GetUser(userId));
        }

        private NotifyMessage? BuildNotificationMessage(VideoProcessingMessage videoMessage)
        {
            return videoMessage.Status switch
            {
                StatusEnum.ProcessadoComSucesso => new NotifyMessage
                {
                    Title = "Video Processing Completed Successfully",
                    Body = $"Your video '{videoMessage.Title}' has been processed successfully and is now available on the platform."
                },
                StatusEnum.ProcessadoComErro => new NotifyMessage
                {
                    Title = "Video Processing Failed",
                    Body = $"There was an error processing your video '{videoMessage.Title}'. Please return to the platform and verify the issue."
                },
                _ => null
            };
        }

        private async Task SendNotificationAsync(UserDto user, NotifyMessage notifyMessage, string videoTitle)
        {
            var emailSent = await Task.FromResult(_userNotification.NotifyUser(user, notifyMessage));

            if (emailSent)
            {
                _logger.LogInformation(
                    "Email notification sent successfully to {Email} for video {VideoTitle}",
                    user.Email, videoTitle);
            }
            else
            {
                _logger.LogError(
                    "Failed to send email notification to {Email} for video {VideoTitle}",
                    user.Email, videoTitle);
            }
        }
    }
}
