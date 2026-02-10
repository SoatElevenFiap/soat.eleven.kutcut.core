using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.application.Interfaces
{
    public interface IVideoMessageFactory
    {
        VideoProcessingMessage? DeserializeVideoMessage(string message);
        NotifyMessage? BuildNotificationMessage(VideoProcessingMessage videoMessage);
    }
}
