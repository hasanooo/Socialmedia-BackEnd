using ApplifyLab.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ApplifyLab.Api.Auth;

/// <summary>
/// The JWT lives only in an httpOnly/secure/SameSite cookie (never localStorage or a JSON
/// response body), so it must be pulled from the cookie into the bearer pipeline manually.
/// Also enforces the Redis logout/revocation blacklist on every request.
/// </summary>
public class CookieJwtBearerEvents : JwtBearerEvents
{
    public override Task MessageReceived(MessageReceivedContext context)
    {
        if (context.Request.Cookies.TryGetValue(AuthCookieNames.AccessToken, out var token) && !string.IsNullOrEmpty(token))
        {
            context.Token = token;
        }

        // SignalR can't attach cookies to its WebSocket handshake headers directly; it sends
        // the access token as a query string parameter instead, per Microsoft's documented pattern.
        var accessTokenQuery = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (string.IsNullOrEmpty(context.Token) && !string.IsNullOrEmpty(accessTokenQuery) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessTokenQuery;
        }

        return Task.CompletedTask;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var blacklist = context.HttpContext.RequestServices.GetRequiredService<IJwtBlacklistService>();
        var jti = context.Principal?.FindFirst("jti")?.Value;

        if (!string.IsNullOrEmpty(jti) && await blacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
        {
            context.Fail("Token has been revoked.");
        }
    }
}
