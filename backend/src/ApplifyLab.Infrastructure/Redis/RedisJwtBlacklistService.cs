using ApplifyLab.Application.Interfaces;
using StackExchange.Redis;

namespace ApplifyLab.Infrastructure.Redis;

public class RedisJwtBlacklistService : IJwtBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "blacklist:jwt:";

    public RedisJwtBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task BlacklistAsync(string jti, DateTimeOffset expiresAt, CancellationToken ct)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero) return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(KeyPrefix + jti, "1", ttl);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(KeyPrefix + jti);
    }
}
