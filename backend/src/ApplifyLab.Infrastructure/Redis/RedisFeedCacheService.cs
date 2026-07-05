using System.Text.Json;
using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using StackExchange.Redis;

namespace ApplifyLab.Infrastructure.Redis;

/// <summary>
/// Cache-aside feed pages. Content is cached briefly (short TTL); like/comment counts are
/// NOT trusted from this cache and are always overlaid from the live Redis counters at
/// read time in PostService, so counts stay real-time even while content is cached.
/// </summary>
public class RedisFeedCacheService : IFeedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);

    public RedisFeedCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string PublicKey(string cursorKey) => $"feed:public:cursor:{cursorKey}";
    private static string PrivateKey(Guid userId, string cursorKey) => $"feed:private:{userId}:cursor:{cursorKey}";
    private const string FirstPageMarker = "first";

    public async Task<FeedPageDto?> GetPublicPageAsync(string cursorKey, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(PublicKey(cursorKey));
        return Deserialize(raw);
    }

    public async Task SetPublicPageAsync(string cursorKey, FeedPageDto page, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(PublicKey(cursorKey), JsonSerializer.Serialize(page), Ttl);
    }

    public async Task<FeedPageDto?> GetPrivatePageAsync(Guid userId, string cursorKey, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(PrivateKey(userId, cursorKey));
        return Deserialize(raw);
    }

    public async Task SetPrivatePageAsync(Guid userId, string cursorKey, FeedPageDto page, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(PrivateKey(userId, cursorKey), JsonSerializer.Serialize(page), Ttl);
    }

    public async Task InvalidateFirstPageAsync(Guid authorId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(PublicKey(FirstPageMarker));
        await db.KeyDeleteAsync(PrivateKey(authorId, FirstPageMarker));
    }

    private static FeedPageDto? Deserialize(RedisValue raw)
        => raw.HasValue ? JsonSerializer.Deserialize<FeedPageDto>(raw!) : null;
}
