using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.application.Dtos.Video;

public record VideoResult(
    Guid Id,
    string Title,
    Guid UserId,
    string Filename,
    StatusEnum Status,
    string StatusDescription,
    DateTime CreatedAt,
    DateTime? UpdatedAt);