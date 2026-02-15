using System.Security.Claims;
using soat.eleven.kutcut.application.Interfaces;

namespace soat.eleven.kutcut.core.api.Security;

public class UserContext : IUserContext
{
    public bool IsAuthenticated { get; }
    public Guid UserId { get; }

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        var sub = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out var userId))
        {
            UserId = userId;
            IsAuthenticated = true;
        }
    }
}
