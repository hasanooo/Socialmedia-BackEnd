using ApplifyLab.Api.Auth;
using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApplifyLab.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, ICurrentUserService currentUser, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _currentUser = currentUser;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        SetAuthCookies(result);
        return Ok(result.User);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        SetAuthCookies(result);
        return Ok(result.User);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { error = "Missing refresh token." });

        var result = await _authService.RefreshAsync(refreshToken, ct);
        SetAuthCookies(result);
        return Ok(result.User);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;
        var jti = _currentUser.Jti ?? string.Empty;
        var expiresAt = _currentUser.TokenExpiresAt ?? DateTimeOffset.UtcNow;
        Request.Cookies.TryGetValue(AuthCookieNames.RefreshToken, out var refreshToken);

        await _authService.LogoutAsync(userId, jti, expiresAt, refreshToken ?? string.Empty, ct);

        Response.Cookies.Delete(AuthCookieNames.AccessToken);
        Response.Cookies.Delete(AuthCookieNames.RefreshToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await _authService.GetCurrentUserAsync(_currentUser.UserId!.Value, ct);
        return user is null ? Unauthorized() : Ok(user);
    }

    private void SetAuthCookies(AuthResult result)
    {
        Response.Cookies.Append(AuthCookieNames.AccessToken, result.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = result.AccessTokenExpiresAt,
            Path = "/",
        });

        Response.Cookies.Append(AuthCookieNames.RefreshToken, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            Path = "/api/auth",
        });
    }
}
