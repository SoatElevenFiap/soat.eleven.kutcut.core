using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.infra.queues.MessagesDtos
{
    public class VideoProcessingMessage
    {
        public Guid UserId { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public StatusEnum Status { get; set; }
    }
}
