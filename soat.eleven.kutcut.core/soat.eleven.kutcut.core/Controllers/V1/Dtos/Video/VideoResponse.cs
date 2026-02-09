namespace soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Video;

public record VideoResponse(
    Guid Id,
    string Title,
    Guid UserId,
    string Filename,
    string Status,
    string StatusDescription,
    DateTime CreatedAt,
    DateTime? UpdatedAt);