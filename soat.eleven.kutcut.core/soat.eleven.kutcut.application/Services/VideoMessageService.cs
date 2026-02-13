using Microsoft.Extensions.Logging;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.queues.MessagesDtos;
using System.Text.Json;

namespace soat.eleven.kutcut.application.Services
{
    public class VideoMessageService : IVideoMessageFactory
    {
        private readonly ILogger<VideoMessageService> _logger;

        public VideoMessageService(ILogger<VideoMessageService> logger)
        {
            _logger = logger;
        }

        public VideoProcessingMessage? DeserializeVideoMessage(string message)
        {
            return JsonSerializer.Deserialize<VideoProcessingMessage>(message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public NotifyMessage? BuildNotificationMessage(VideoProcessingMessage videoMessage)
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
    }
}
