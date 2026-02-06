using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Models
{
    public class NotificationModel
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? VideoId { get; set; }
        public NotificationTypeEnum? Type { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual VideoModel? Video { get; set; }
        public virtual NotificationTypeModel? TypeNavigation { get; set; }
    }
}
