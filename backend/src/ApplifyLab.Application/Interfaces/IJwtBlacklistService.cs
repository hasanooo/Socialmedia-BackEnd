namespace ApplifyLab.Application.Interfaces;

public interface IJwtBlacklistService
{
    Task BlacklistAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct);
}
