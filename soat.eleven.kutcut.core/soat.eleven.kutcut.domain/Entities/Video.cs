using FluentResults;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.domain.Entities
{
    public class Video
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; } = null!;
        public Guid UserId { get; private set; }
        public string Filename { get; private set; } = null!;
        public StatusEnum Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        protected Video() { }

        private Video(Guid id, string title, Guid userId, string filename)
        {
            Id = id;
            Title = title;
            UserId = userId;
            Filename = filename;
            Status = StatusEnum.Pendente;
            CreatedAt = DateTime.UtcNow;
        }

        public static Result<Video> Create(string? title, Guid userId, string? filename)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(title))
                errors.Add("O título do vídeo é obrigatório.");

            if (userId == Guid.Empty)
                errors.Add("O identificador do usuário é obrigatório.");

            if (string.IsNullOrWhiteSpace(filename))
                errors.Add("O nome do arquivo é obrigatório.");

            if (errors.Count > 0)
                return Result.Fail<Video>(errors);

            var video = new Video(Guid.NewGuid(), title!, userId, filename!);
            return Result.Ok(video);
        }

        public Result UpdateTitle(string? newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
                return Result.Fail("O título do vídeo é obrigatório.");

            Title = newTitle;
            UpdatedAt = DateTime.UtcNow;
            return Result.Ok();
        }

        public void SetStatus(StatusEnum status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
