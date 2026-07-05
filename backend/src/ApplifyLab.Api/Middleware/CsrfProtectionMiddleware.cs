namespace ApplifyLab.Api.Middleware;

/// <summary>
/// CSRF mitigation for the cookie-based JWT: browsers only attach a custom header on
/// same-origin (or explicitly CORS-allowed) requests, and simple cross-site form
/// submissions can't set one. Combined with SameSite=Strict on the auth cookie and CORS
/// locked to the frontend origin, this blocks classic CSRF without needing a separate
/// antiforgery token round-trip.
/// </summary>
public class CsrfProtectionMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS" };
    private const string RequiredHeader = "X-Requested-With";
    private const string RequiredValue = "XMLHttpRequest";

    private readonly RequestDelegate _next;

    public CsrfProtectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (SafeMethods.Contains(context.Request.Method) || context.Request.Path.StartsWithSegments("/hubs"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(RequiredHeader, out var value) || value != RequiredValue)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid CSRF header." });
            return;
        }

        await _next(context);
    }
}
