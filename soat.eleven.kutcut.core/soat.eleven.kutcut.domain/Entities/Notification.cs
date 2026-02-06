using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VideoId { get; set; }
        public NotificationTypeEnum? Type { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
