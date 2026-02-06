using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.infra.Models
{
    public class StatusModel
    {
        public StatusEnum Id { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<VideoModel> Videos { get; set; } = new List<VideoModel>();
    }
}
