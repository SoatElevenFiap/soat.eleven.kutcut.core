using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Models
{
    public class NotificationTypeModel
    {
        public NotificationTypeEnum Id { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<NotificationModel> Notifications { get; set; } = new List<NotificationModel>();
    }
}
