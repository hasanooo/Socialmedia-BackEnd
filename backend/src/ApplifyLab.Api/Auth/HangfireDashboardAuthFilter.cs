using Hangfire.Dashboard;

namespace ApplifyLab.Api.Auth;

/// <summary>
/// Dev-friendly Hangfire dashboard guard: open in Development, otherwise requires a
/// shared key (Hangfire:DashboardKey) passed as ?key=... since this is a demo project
/// without an admin-role system.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly string _requiredKey;
    private readonly bool _isDevelopment;

    public HangfireDashboardAuthFilter(string requiredKey, bool isDevelopment)
    {
        _requiredKey = requiredKey;
        _isDevelopment = isDevelopment;
    }

    public bool Authorize(DashboardContext context)
    {
        if (_isDevelopment) return true;

        var httpContext = context.GetHttpContext();
        return httpContext.Request.Query["key"] == _requiredKey;
    }
}
