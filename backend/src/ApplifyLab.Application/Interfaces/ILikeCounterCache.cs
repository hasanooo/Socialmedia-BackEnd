using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.Interfaces;

/// <summary>
/// Redis-backed like dedupe (SET) + counters. This is the O(1) hot path that keeps
/// like/unlike off the Postgres row entirely; Postgres is reconciled asynchronously.
/// </summary>
public interface ILikeCounterCache
{
    Task<bool> IsLikedAsync(LikeableType type, Guid id, Guid userId, CancellationToken ct);
    Task<(bool liked, long count)> ToggleAsync(LikeableType type, Guid id, Guid userId, CancellationToken ct);
    Task<long> GetCountAsync(LikeableType type, Guid id, CancellationToken ct);
    Task<Dictionary<Guid, long>> GetCountsAsync(LikeableType type, IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<HashSet<Guid>> GetLikedIdsAsync(LikeableType type, IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct);
    Task SeedCountAsync(LikeableType type, Guid id, long count, CancellationToken ct);
}

public interface ICommentCounterCache
{
    Task<long> IncrementAsync(Guid postId, CancellationToken ct);
    Task<long> DecrementAsync(Guid postId, long by, CancellationToken ct);
    Task<long> GetCountAsync(Guid postId, CancellationToken ct);
    Task<Dictionary<Guid, long>> GetCountsAsync(IReadOnlyCollection<Guid> postIds, CancellationToken ct);
    Task SeedCountAsync(Guid postId, long count, CancellationToken ct);
}
