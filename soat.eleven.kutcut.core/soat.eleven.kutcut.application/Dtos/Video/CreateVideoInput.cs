namespace soat.eleven.kutcut.application.Dtos.Video;

public record CreateVideoInput(
    string? Title, 
    Guid UserId, 
    string? FileName, 
    Stream FileStream);