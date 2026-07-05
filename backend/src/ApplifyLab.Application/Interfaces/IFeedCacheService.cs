using ApplifyLab.Application.DTOs;

namespace ApplifyLab.Application.Interfaces;

public interface IFeedCacheService
{
    Task<FeedPageDto?> GetPublicPageAsync(string cursorKey, CancellationToken ct);
    Task SetPublicPageAsync(string cursorKey, FeedPageDto page, CancellationToken ct);
    Task<FeedPageDto?> GetPrivatePageAsync(Guid userId, string cursorKey, CancellationToken ct);
    Task SetPrivatePageAsync(Guid userId, string cursorKey, FeedPageDto page, CancellationToken ct);
    Task InvalidateFirstPageAsync(Guid authorId, CancellationToken ct);
}
