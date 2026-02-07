using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.queues.Interfaces;

namespace soat.eleven.kutcut.application.NotificationService
{
    public class BackgroundNotificationService : BackgroundService
    {
        private readonly ILogger<BackgroundNotificationService> _logger;
        private readonly IMessageListener _messageListener;
        private readonly IVideoNotificationProcessor _videoNotificationProcessor;
        private readonly RabbitMQSettings _rabbitMQSettings;

        public BackgroundNotificationService(
            ILogger<BackgroundNotificationService> logger,
            IMessageListener messageListener,
            IVideoNotificationProcessor videoNotificationProcessor,
            IOptions<RabbitMQSettings> rabbitMQSettings)
        {
            _logger = logger;
            _messageListener = messageListener;
            _videoNotificationProcessor = videoNotificationProcessor;
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
                        async (message) => await _videoNotificationProcessor.ProcessVideoNotificationAsync(message));

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

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Notification Service is stopping");
            _messageListener.StopListening();
            await base.StopAsync(stoppingToken);
        }
    }
}
