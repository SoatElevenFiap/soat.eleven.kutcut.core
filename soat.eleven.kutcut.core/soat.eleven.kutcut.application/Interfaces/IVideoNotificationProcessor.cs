using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.application.Interfaces
{
    public interface IVideoNotificationProcessor
    {
        Task ProcessVideoNotificationAsync(VideoProcessingMessage message);
    }
}
