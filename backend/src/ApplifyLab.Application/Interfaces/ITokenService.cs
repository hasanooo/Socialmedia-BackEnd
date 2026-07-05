using System.Security.Claims;
using ApplifyLab.Domain.Entities;

namespace ApplifyLab.Application.Interfaces;

public record AccessTokenResult(string Token, string Jti, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
    ClaimsPrincipal? ValidateAccessToken(string token, bool validateLifetime = true);
}
