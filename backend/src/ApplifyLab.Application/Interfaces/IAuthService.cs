using ApplifyLab.Application.DTOs;

namespace ApplifyLab.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(Guid userId, string accessTokenJti, DateTimeOffset accessTokenExpiresAt, string refreshToken, CancellationToken ct);
    Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct);
}
