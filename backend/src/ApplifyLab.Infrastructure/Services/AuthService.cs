using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Entities;
using ApplifyLab.Infrastructure.Options;
using ApplifyLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApplifyLab.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IJwtBlacklistService _blacklist;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IJwtBlacklistService blacklist,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _blacklist = blacklist;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists) throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken);
        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        existing.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(existing.User, ct);
    }

    public async Task LogoutAsync(Guid userId, string accessTokenJti, DateTimeOffset accessTokenExpiresAt, string refreshToken, CancellationToken ct)
    {
        await _blacklist.BlacklistAsync(accessTokenJti, accessTokenExpiresAt, ct);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = _tokenService.HashRefreshToken(refreshToken);
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash && rt.UserId == userId, ct);
            if (existing is not null)
            {
                existing.RevokedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? null : ToDto(user);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _tokenService.GenerateAccessToken(user);
        var refreshRaw = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        return new AuthResult(ToDto(user), access.Token, refreshRaw, access.ExpiresAt);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);
}
