namespace soat.eleven.kutcut.application.Dtos.Video;

public record CreateVideoInput(
    string? Title, 
    string? FileName, 
    Stream FileStream);