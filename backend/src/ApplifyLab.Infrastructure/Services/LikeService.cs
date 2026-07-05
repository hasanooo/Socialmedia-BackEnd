using ApplifyLab.Application.Common;
using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Entities;
using ApplifyLab.Domain.Enums;
using ApplifyLab.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApplifyLab.Infrastructure.Services;

public class LikeService : ILikeService
{
    private readonly AppDbContext _db;
    private readonly ILikeCounterCache _likeCounters;
    private readonly IRealtimeNotifier _realtime;
    private readonly IBackgroundJobClient _backgroundJobs;

    private const int MaxLimit = 50;

    public LikeService(AppDbContext db, ILikeCounterCache likeCounters, IRealtimeNotifier realtime, IBackgroundJobClient backgroundJobs)
    {
        _db = db;
        _likeCounters = likeCounters;
        _realtime = realtime;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<ToggleLikeResult> ToggleAsync(Guid userId, ToggleLikeRequest request, CancellationToken ct)
    {
        await EnsureAccessibleAsync(request.LikeableType, request.LikeableId, userId, ct);

        // Hot path: Redis SET dedupe + counter, no Postgres write on the request thread.
        var (liked, count) = await _likeCounters.ToggleAsync(request.LikeableType, request.LikeableId, userId, ct);

        // Durable write-behind to Postgres happens off the request thread.
        _backgroundJobs.Enqueue<ILikeSyncJob>(job => job.SyncLikeAsync(request.LikeableType, request.LikeableId, userId, liked, CancellationToken.None));

        await _realtime.NotifyLikeChangedAsync(request.LikeableType, request.LikeableId, count, ct);

        return new ToggleLikeResult(liked, count);
    }

    public async Task<LikersPageDto> GetLikersAsync(LikeableType likeableType, Guid likeableId, string? cursor, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var query = _db.Likes.AsNoTracking()
            .Include(l => l.User)
            .Where(l => l.LikeableType == likeableType && l.LikeableId == likeableId);

        var decoded = Cursor.Decode(cursor);
        if (decoded is { } c)
        {
            query = query.Where(l => l.CreatedAt < c.createdAt
                || (l.CreatedAt == c.createdAt && l.SequenceNumber < c.sequenceNumber));
        }

        var rows = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.SequenceNumber)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        var items = page.Select(l => new LikerDto(l.UserId, l.User.FullName, l.CreatedAt)).ToList();
        var nextCursor = hasMore && page.Count > 0
            ? Cursor.Encode(page[^1].CreatedAt, page[^1].SequenceNumber)
            : null;

        return new LikersPageDto(items, nextCursor);
    }

    private async Task EnsureAccessibleAsync(LikeableType type, Guid id, Guid userId, CancellationToken ct)
    {
        if (type == LikeableType.Post)
        {
            var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new KeyNotFoundException("Post not found.");
            if (post.Visibility == PostVisibility.Private && post.AuthorId != userId)
                throw new UnauthorizedAccessException("Cannot like a private post you don't own.");
        }
        else
        {
            var comment = await _db.Comments.AsNoTracking().Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new KeyNotFoundException("Comment not found.");
            if (comment.Post.Visibility == PostVisibility.Private && comment.Post.AuthorId != userId)
                throw new UnauthorizedAccessException("Cannot like a comment on a private post you don't own.");
        }
    }
}
