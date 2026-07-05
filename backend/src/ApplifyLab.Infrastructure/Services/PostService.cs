using ApplifyLab.Application.Common;
using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Entities;
using ApplifyLab.Domain.Enums;
using ApplifyLab.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApplifyLab.Infrastructure.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ILikeCounterCache _likeCounters;
    private readonly ICommentCounterCache _commentCounters;
    private readonly IFeedCacheService _feedCache;
    private readonly IBackgroundJobClient _backgroundJobs;

    private const int MaxLimit = 50;

    public PostService(
        AppDbContext db,
        IFileStorageService fileStorage,
        ILikeCounterCache likeCounters,
        ICommentCounterCache commentCounters,
        IFeedCacheService feedCache,
        IBackgroundJobClient backgroundJobs)
    {
        _db = db;
        _fileStorage = fileStorage;
        _likeCounters = likeCounters;
        _commentCounters = commentCounters;
        _feedCache = feedCache;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<PostDto> CreateAsync(Guid authorId, CreatePostRequest request, Stream? image, string? imageFileName, string? imageContentType, CancellationToken ct)
    {
        var author = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == authorId, ct)
            ?? throw new InvalidOperationException("Author not found.");

        string? imageUrl = null;
        string? imageKey = null;
        if (image is not null)
        {
            var uploaded = await _fileStorage.UploadAsync(image, imageFileName ?? "upload", imageContentType ?? "application/octet-stream", ct);
            imageUrl = uploaded.Url;
            imageKey = uploaded.Key;
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Content = request.Content,
            ImageUrl = imageUrl,
            Visibility = request.Visibility,
            LikeCount = 0,
            CommentCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);

        await _likeCounters.SeedCountAsync(LikeableType.Post, post.Id, 0, ct);
        await _commentCounters.SeedCountAsync(post.Id, 0, ct);

        if (imageKey is not null)
        {
            _backgroundJobs.Enqueue<IThumbnailJob>(job => job.GenerateThumbnailAsync(post.Id, imageKey, CancellationToken.None));
        }

        await _feedCache.InvalidateFirstPageAsync(authorId, ct);

        return new PostDto(
            post.Id,
            new PostAuthorDto(author.Id, author.FullName),
            post.Content,
            post.ImageUrl,
            post.ThumbnailUrl,
            post.Visibility,
            0,
            0,
            false,
            post.CreatedAt);
    }

    public async Task<FeedPageDto> GetFeedAsync(Guid? currentUserId, string? cursor, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);
        var cursorKey = cursor ?? "first";

        var cached = currentUserId.HasValue
            ? await _feedCache.GetPrivatePageAsync(currentUserId.Value, cursorKey, ct)
            : await _feedCache.GetPublicPageAsync(cursorKey, ct);

        FeedPageDto page;
        if (cached is not null)
        {
            page = cached;
        }
        else
        {
            page = await LoadFeedPageFromDbAsync(currentUserId, cursor, limit, ct);

            if (currentUserId.HasValue)
                await _feedCache.SetPrivatePageAsync(currentUserId.Value, cursorKey, page, ct);
            else
                await _feedCache.SetPublicPageAsync(cursorKey, page, ct);
        }

        return await OverlayLiveCountsAsync(page, currentUserId, ct);
    }

    private async Task<FeedPageDto> LoadFeedPageFromDbAsync(Guid? currentUserId, string? cursor, int limit, CancellationToken ct)
    {
        var query = _db.Posts.AsNoTracking().Include(p => p.Author).AsQueryable();

        query = currentUserId.HasValue
            ? query.Where(p => p.Visibility == PostVisibility.Public || p.AuthorId == currentUserId.Value)
            : query.Where(p => p.Visibility == PostVisibility.Public);

        var decoded = Cursor.Decode(cursor);
        if (decoded is { } c)
        {
            query = query.Where(p => p.CreatedAt < c.createdAt
                || (p.CreatedAt == c.createdAt && p.SequenceNumber < c.sequenceNumber));
        }

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.SequenceNumber)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();

        var items = page.Select(p => new PostDto(
            p.Id,
            new PostAuthorDto(p.AuthorId, p.Author.FullName),
            p.Content,
            p.ImageUrl,
            p.ThumbnailUrl,
            p.Visibility,
            p.LikeCount,
            p.CommentCount,
            false,
            p.CreatedAt)).ToList();

        var nextCursor = hasMore && page.Count > 0
            ? Cursor.Encode(page[^1].CreatedAt, page[^1].SequenceNumber)
            : null;

        return new FeedPageDto(items, nextCursor);
    }

    private async Task<FeedPageDto> OverlayLiveCountsAsync(FeedPageDto page, Guid? currentUserId, CancellationToken ct)
    {
        if (page.Items.Count == 0) return page;

        var ids = page.Items.Select(p => p.Id).ToList();
        var likeCounts = await _likeCounters.GetCountsAsync(LikeableType.Post, ids, ct);
        var commentCounts = await _commentCounters.GetCountsAsync(ids, ct);
        var likedIds = currentUserId.HasValue
            ? await _likeCounters.GetLikedIdsAsync(LikeableType.Post, ids, currentUserId.Value, ct)
            : new HashSet<Guid>();

        var items = page.Items.Select(p => p with
        {
            LikeCount = likeCounts.GetValueOrDefault(p.Id, p.LikeCount),
            CommentCount = commentCounts.GetValueOrDefault(p.Id, p.CommentCount),
            LikedByCurrentUser = likedIds.Contains(p.Id),
        }).ToList();

        return page with { Items = items };
    }

    public async Task<PostDto?> GetByIdAsync(Guid postId, Guid? currentUserId, CancellationToken ct)
    {
        var post = await _db.Posts.AsNoTracking().Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is null) return null;
        if (post.Visibility == PostVisibility.Private && post.AuthorId != currentUserId) return null;

        var likeCount = await _likeCounters.GetCountAsync(LikeableType.Post, post.Id, ct);
        var commentCount = await _commentCounters.GetCountAsync(post.Id, ct);
        var liked = currentUserId.HasValue && await _likeCounters.IsLikedAsync(LikeableType.Post, post.Id, currentUserId.Value, ct);

        return new PostDto(
            post.Id,
            new PostAuthorDto(post.AuthorId, post.Author.FullName),
            post.Content,
            post.ImageUrl,
            post.ThumbnailUrl,
            post.Visibility,
            likeCount,
            commentCount,
            liked,
            post.CreatedAt);
    }

    public async Task DeleteAsync(Guid userId, Guid postId, CancellationToken ct)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own posts.");

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync(ct);
        await _feedCache.InvalidateFirstPageAsync(userId, ct);
    }
}
