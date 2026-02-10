namespace soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Common;

public record PaginationQuery(
    int PageNumber = 1, 
    int PageSize = 10);