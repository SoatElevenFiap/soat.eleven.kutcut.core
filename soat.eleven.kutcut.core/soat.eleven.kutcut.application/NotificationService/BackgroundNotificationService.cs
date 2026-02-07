using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.queues.Interfaces;
using System.Text.Json;

namespace soat.eleven.kutcut.application.NotificationService
{
    public class BackgroundNotificationService : BackgroundService
    {
        private readonly ILogger<BackgroundNotificationService> _logger;
        private readonly IMessageListener _messageListener;
        private readonly IUserSerivce _userService;
        private readonly IUserNotificaton _userNotification;
        private readonly RabbitMQSettings _rabbitMQSettings;

        public BackgroundNotificationService(
            ILogger<BackgroundNotificationService> logger,
            IMessageListener messageListener,
            IUserSerivce userService,
            IUserNotificaton userNotification,
            IOptions<RabbitMQSettings> rabbitMQSettings)
        {
            _logger = logger;
            _messageListener = messageListener;
            _userService = userService;
            _userNotification = userNotification;
            _rabbitMQSettings = rabbitMQSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Notification Service is starting");

            try
            {
                await Task.Run(() =>
                {
                    _messageListener.StartListening(
                        _rabbitMQSettings.VideoProcessingQueueName,
                        async (message) => await ProcessVideoNotificationAsync(message));

                    _logger.LogInformation("Listening to queue: {QueueName}", 
                        _rabbitMQSettings.VideoProcessingQueueName);
                }, stoppingToken);

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Background Notification Service");
                throw;
            }
        }

        private async Task ProcessVideoNotificationAsync(string message)
        {
            try
            {
                _logger.LogInformation("Processing video notification message: {Message}", message);

                var videoMessage = JsonSerializer.Deserialize<VideoProcessingMessage>(message, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (videoMessage == null)
                {
                    _logger.LogError("Failed to deserialize video processing message");
                    return;
                }

                // Retrieve user information
                var user = _userService.GetUser(videoMessage.UserId);

                if (user == null)
                {
                    _logger.LogError("User not found: {UserId}", videoMessage.UserId);
                    return;
                }

                // Prepare notification message based on video status
                var notifyMessage = new NotifyMessage();

                switch (videoMessage.Status)
                {
                    case StatusEnum.ProcessadoComSucesso:
                        notifyMessage.Title = "Video Processing Completed Successfully";
                        notifyMessage.Body = $"Your video '{videoMessage.Title}' has been processed successfully and is now available on the platform.";
                        break;

                    case StatusEnum.ProcessadoComErro:
                        notifyMessage.Title = "Video Processing Failed";
                        notifyMessage.Body = $"There was an error processing your video '{videoMessage.Title}'. Please return to the platform and verify the issue.";
                        break;

                    default:
                        _logger.LogWarning("Unknown video status: {Status}", videoMessage.Status);
                        return;
                }

                // Send email notification
                var emailSent = _userNotification.NotifyUser(user, notifyMessage);

                if (emailSent)
                {
                    _logger.LogInformation(
                        "Email notification sent successfully to {Email} for video {VideoTitle}",
                        user.Email, videoMessage.Title);
                }
                else
                {
                    _logger.LogError(
                        "Failed to send email notification to {Email} for video {VideoTitle}",
                        user.Email, videoMessage.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video notification message");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Notification Service is stopping");
            _messageListener.StopListening();
            await base.StopAsync(stoppingToken);
        }
    }
}
