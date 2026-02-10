namespace soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Common;

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);