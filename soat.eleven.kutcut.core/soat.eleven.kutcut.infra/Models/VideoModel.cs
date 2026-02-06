using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Models
{
    public class VideoModel
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public Guid UserId { get; set; }
        public string Filename { get; set; } = null!;
        public StatusEnum? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual StatusModel? StatusNavigation { get; set; }
        public virtual ICollection<NotificationModel> Notifications { get; set; } = new List<NotificationModel>();
    }
}
