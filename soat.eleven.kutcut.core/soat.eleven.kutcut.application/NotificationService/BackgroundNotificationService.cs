using Microsoft.Extensions.DependencyInjection;
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
        private readonly IVideoMessageFactory _videoMessageFactory;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly IServiceScopeFactory _scopedFactory;
        private IVideoService _videoService => _scopedFactory.CreateScope().ServiceProvider.GetRequiredService<IVideoService>();

        public BackgroundNotificationService(
            ILogger<BackgroundNotificationService> logger,
            IMessageListener messageListener,
            IVideoNotificationProcessor videoNotificationProcessor,
            IOptions<RabbitMQSettings> rabbitMQSettings,
            IVideoMessageFactory videoMessageFactory,
            IServiceScopeFactory scopedFactory
            )
        {
            _logger = logger;
            _messageListener = messageListener;
            _videoNotificationProcessor = videoNotificationProcessor;
            _rabbitMQSettings = rabbitMQSettings.Value;
            _videoMessageFactory = videoMessageFactory;
            _scopedFactory = scopedFactory;

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
                        async (message) => await ProcessVideoMessage(message));

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

        private async Task ProcessVideoMessage(string message)
        {
            try
            {
                var videoMessage = _videoMessageFactory.DeserializeVideoMessage(message);
                if (videoMessage == null)
                    throw new ArgumentException("Mensagem recebida na fila está nula, favor verificar");

                var result = await _videoService.UpdateStatusAsync(videoMessage.MessageId, videoMessage.Status);

                if (result.IsSuccess)
                    await _videoNotificationProcessor.ProcessVideoNotificationAsync(videoMessage);
                else
                {
                    _logger.LogError("Failed to update video status for message {MessageId}: {Errors}", videoMessage.MessageId, string.Join(", ", result.Errors));
                    throw new Exception($"Failed to update video status for message {videoMessage.MessageId}: {string.Join(", ", result.Errors)}");

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar processar mensagem recebia via mensageria");
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
