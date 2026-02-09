namespace soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Common;

public record ErrorResponse(
    string Title, 
    int Status, 
    IEnumerable<string> Errors);