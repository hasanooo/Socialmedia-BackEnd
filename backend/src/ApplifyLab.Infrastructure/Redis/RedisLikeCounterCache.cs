using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Enums;
using StackExchange.Redis;

namespace ApplifyLab.Infrastructure.Redis;

/// <summary>
/// O(1) like dedupe (Redis SET) + instant counters, keeping hot-row like/unlike traffic
/// off Postgres entirely. Postgres is the durable source of truth, updated asynchronously.
/// </summary>
public class RedisLikeCounterCache : ILikeCounterCache, ICommentCounterCache
{
    private readonly IConnectionMultiplexer _redis;

    public RedisLikeCounterCache(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    private static string SetKey(LikeableType type, Guid id) => $"likes:{type.ToString().ToLowerInvariant()}:{id}";
    private static string CounterKey(LikeableType type, Guid id) => $"counter:like:{type.ToString().ToLowerInvariant()}:{id}";
    private static string CommentCounterKey(Guid postId) => $"counter:comment:post:{postId}";

    public async Task<bool> IsLikedAsync(LikeableType type, Guid id, Guid userId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        return await db.SetContainsAsync(SetKey(type, id), userId.ToString());
    }

    public async Task<(bool liked, long count)> ToggleAsync(LikeableType type, Guid id, Guid userId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var setKey = SetKey(type, id);
        var counterKey = CounterKey(type, id);
        var member = userId.ToString();

        var added = await db.SetAddAsync(setKey, member);
        if (added)
        {
            var newCount = await db.StringIncrementAsync(counterKey);
            return (true, newCount);
        }

        var removed = await db.SetRemoveAsync(setKey, member);
        if (removed)
        {
            var newCount = await db.StringDecrementAsync(counterKey);
            if (newCount < 0)
            {
                await db.StringSetAsync(counterKey, 0);
                newCount = 0;
            }
            return (false, newCount);
        }

        // Shouldn't happen, but stay consistent if the set mutated concurrently.
        var current = await db.StringGetAsync(counterKey);
        return (false, current.HasValue ? (long)current : 0);
    }

    public async Task<long> GetCountAsync(LikeableType type, Guid id, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CounterKey(type, id));
        return value.HasValue ? (long)value : 0;
    }

    public async Task<Dictionary<Guid, long>> GetCountsAsync(LikeableType type, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<Guid, long>();
        var db = _redis.GetDatabase();
        var idList = ids.ToList();
        var keys = idList.Select(id => (RedisKey)CounterKey(type, id)).ToArray();
        var values = await db.StringGetAsync(keys);

        var result = new Dictionary<Guid, long>();
        for (var i = 0; i < idList.Count; i++)
        {
            result[idList[i]] = values[i].HasValue ? (long)values[i] : 0;
        }
        return result;
    }

    public async Task<HashSet<Guid>> GetLikedIdsAsync(LikeableType type, IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct)
    {
        if (ids.Count == 0) return new HashSet<Guid>();
        var db = _redis.GetDatabase();
        var member = userId.ToString();
        var idList = ids.ToList();

        var batch = db.CreateBatch();
        var tasks = idList.Select(id => batch.SetContainsAsync(SetKey(type, id), member)).ToArray();
        batch.Execute();
        var results = await Task.WhenAll(tasks);

        var liked = new HashSet<Guid>();
        for (var i = 0; i < idList.Count; i++)
        {
            if (results[i]) liked.Add(idList[i]);
        }
        return liked;
    }

    public async Task SeedCountAsync(LikeableType type, Guid id, long count, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(CounterKey(type, id), count, when: When.NotExists);
    }

    public async Task<long> IncrementAsync(Guid postId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        return await db.StringIncrementAsync(CommentCounterKey(postId));
    }

    public async Task<long> DecrementAsync(Guid postId, long by, CancellationToken ct)
    {
        if (by <= 0) return await GetCountAsync(postId, ct);
        var db = _redis.GetDatabase();
        var newCount = await db.StringDecrementAsync(CommentCounterKey(postId), by);
        if (newCount < 0)
        {
            await db.StringSetAsync(CommentCounterKey(postId), 0);
            newCount = 0;
        }
        return newCount;
    }

    public async Task<long> GetCountAsync(Guid postId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(CommentCounterKey(postId));
        return value.HasValue ? (long)value : 0;
    }

    public async Task<Dictionary<Guid, long>> GetCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct)
    {
        if (postIds.Count == 0) return new Dictionary<Guid, long>();
        var db = _redis.GetDatabase();
        var idList = postIds.ToList();
        var keys = idList.Select(id => (RedisKey)CommentCounterKey(id)).ToArray();
        var values = await db.StringGetAsync(keys);

        var result = new Dictionary<Guid, long>();
        for (var i = 0; i < idList.Count; i++)
        {
            result[idList[i]] = values[i].HasValue ? (long)values[i] : 0;
        }
        return result;
    }

    public async Task SeedCountAsync(Guid postId, long count, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(CommentCounterKey(postId), count, when: When.NotExists);
    }
}
