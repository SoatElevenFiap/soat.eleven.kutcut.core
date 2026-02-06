using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.domain.Entities
{
    public class Video
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public Guid UserId { get; set; }
        public string Filename { get; set; } = null!;
        public StatusEnum? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
