namespace soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Video;

public record CreateVideoRequest(
    string? Title, 
    Guid UserId);