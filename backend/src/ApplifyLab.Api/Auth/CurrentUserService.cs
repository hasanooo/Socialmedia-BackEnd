using System.Security.Claims;
using ApplifyLab.Application.Interfaces;

namespace ApplifyLab.Api.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Jti => User?.FindFirstValue("jti");

    public DateTimeOffset? TokenExpiresAt
    {
        get
        {
            var exp = User?.FindFirstValue("exp");
            return long.TryParse(exp, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
        }
    }
}
