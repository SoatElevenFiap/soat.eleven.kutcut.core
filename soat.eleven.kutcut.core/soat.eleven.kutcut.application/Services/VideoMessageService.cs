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
            try
            {
                return JsonSerializer.Deserialize<VideoProcessingMessage>(message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize video processing message: {Message}", message);
                return null;
            }
        }

        public NotifyMessage? BuildNotificationMessage(VideoProcessingMessage videoMessage)
        {
            return videoMessage.Status switch
            {
                StatusEnum.ProcessadoComSucesso => new NotifyMessage
                {
                    Title = "Processamento de Vídeo Concluído",
                    Body = $"Seu vídeo '{videoMessage.Title}' foi processado com sucesso e suas imagens já estão disponíveis na plataforma."
                },
                StatusEnum.ProcessadoComErro => new NotifyMessage
                {
                    Title = "Falha no Processamento de Vídeo",
                    Body = $"Ocorreu um erro ao processar seu vídeo '{videoMessage.Title}'. Retorne à plataforma e verifique o problema."
                },
                _ => null
            };
        }
    }
}
