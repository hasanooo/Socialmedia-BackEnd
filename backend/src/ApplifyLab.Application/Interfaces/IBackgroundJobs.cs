using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.Interfaces;

/// <summary>
/// Enqueued via Hangfire after an image upload so the user-facing request never blocks on
/// resizing. Downloads the original from object storage, generates a thumbnail, re-uploads it.
/// </summary>
public interface IThumbnailJob
{
    Task GenerateThumbnailAsync(Guid postId, string sourceKey, CancellationToken ct);
}

/// <summary>
/// Write-behind persistence for likes: the Redis SET/counter update is synchronous and instant,
/// this job durably reflects that single like/unlike into Postgres (row + denormalized count)
/// asynchronously so hot posts never contend on a Postgres row for every click.
/// </summary>
public interface ILikeSyncJob
{
    Task SyncLikeAsync(LikeableType type, Guid id, Guid userId, bool liked, CancellationToken ct);
}

/// <summary>
/// Recurring job that diffs Redis like sets/counters against Postgres to correct any drift
/// (e.g. from a crashed job or Redis eviction) — Postgres remains the durable source of truth.
/// </summary>
public interface ILikeReconciliationJob
{
    Task ReconcileAsync(CancellationToken ct);
}
