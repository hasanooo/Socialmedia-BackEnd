using ApplifyLab.Application.Common;
using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Entities;
using ApplifyLab.Domain.Enums;
using ApplifyLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApplifyLab.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly ILikeCounterCache _likeCounters;
    private readonly ICommentCounterCache _commentCounters;
    private readonly IRealtimeNotifier _realtime;

    private const int MaxLimit = 50;

    public CommentService(AppDbContext db, ILikeCounterCache likeCounters, ICommentCounterCache commentCounters, IRealtimeNotifier realtime)
    {
        _db = db;
        _likeCounters = likeCounters;
        _commentCounters = commentCounters;
        _realtime = realtime;
    }

    public async Task<CommentDto> CreateAsync(Guid authorId, Guid postId, CreateCommentRequest request, CancellationToken ct)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.Visibility == PostVisibility.Private && post.AuthorId != authorId)
            throw new UnauthorizedAccessException("Cannot comment on a private post you don't own.");

        if (request.ParentCommentId.HasValue)
        {
            var parentExists = await _db.Comments.AnyAsync(c => c.Id == request.ParentCommentId.Value && c.PostId == postId, ct);
            if (!parentExists) throw new InvalidOperationException("Parent comment not found on this post.");
        }

        var author = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == authorId, ct);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = authorId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content,
            LikeCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Comments.Add(comment);
        post.CommentCount += 1;
        await _db.SaveChangesAsync(ct);

        await _likeCounters.SeedCountAsync(LikeableType.Comment, comment.Id, 0, ct);
        await _commentCounters.IncrementAsync(postId, ct);

        var dto = new CommentDto(
            comment.Id, comment.PostId, new PostAuthorDto(author.Id, author.FullName),
            comment.ParentCommentId, comment.Content, 0, false, comment.CreatedAt,
            Array.Empty<CommentDto>());

        await _realtime.NotifyCommentAddedAsync(postId, dto, ct);
        return dto;
    }

    public async Task<CommentPageDto> GetForPostAsync(Guid postId, Guid? currentUserId, string? cursor, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct)
            ?? throw new KeyNotFoundException("Post not found.");
        if (post.Visibility == PostVisibility.Private && post.AuthorId != currentUserId)
            throw new UnauthorizedAccessException("Cannot view comments on a private post you don't own.");

        var query = _db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.PostId == postId && c.ParentCommentId == null);

        var decoded = Cursor.Decode(cursor);
        if (decoded is { } c2)
        {
            query = query.Where(c => c.CreatedAt < c2.createdAt
                || (c.CreatedAt == c2.createdAt && c.SequenceNumber < c2.sequenceNumber));
        }

        var topLevel = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.SequenceNumber)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = topLevel.Count > limit;
        var page = topLevel.Take(limit).ToList();
        var topLevelIds = page.Select(c => c.Id).ToList();

        var replies = topLevelIds.Count == 0
            ? new List<Comment>()
            : await _db.Comments.AsNoTracking()
                .Include(c => c.Author)
                .Where(c => c.ParentCommentId != null && topLevelIds.Contains(c.ParentCommentId!.Value))
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct);

        var allIds = page.Select(c => c.Id).Concat(replies.Select(r => r.Id)).ToList();
        var likeCounts = await _likeCounters.GetCountsAsync(LikeableType.Comment, allIds, ct);
        var likedIds = currentUserId.HasValue
            ? await _likeCounters.GetLikedIdsAsync(LikeableType.Comment, allIds, currentUserId.Value, ct)
            : new HashSet<Guid>();

        var repliesByParent = replies.ToLookup(r => r.ParentCommentId!.Value);

        CommentDto ToDto(Comment c) => new(
            c.Id, c.PostId, new PostAuthorDto(c.AuthorId, c.Author.FullName), c.ParentCommentId,
            c.Content, likeCounts.GetValueOrDefault(c.Id, c.LikeCount), likedIds.Contains(c.Id), c.CreatedAt,
            repliesByParent[c.Id].OrderBy(r => r.CreatedAt).Select(ToDto).ToList());

        var items = page.Select(ToDto).ToList();
        var nextCursor = hasMore && page.Count > 0
            ? Cursor.Encode(page[^1].CreatedAt, page[^1].SequenceNumber)
            : null;

        return new CommentPageDto(items, nextCursor);
    }

    public async Task DeleteAsync(Guid userId, Guid commentId, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId, ct)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments.");

        var post = await _db.Posts.FirstAsync(p => p.Id == comment.PostId, ct);
        var replyCount = await _db.Comments.CountAsync(c => c.ParentCommentId == commentId, ct);

        _db.Comments.RemoveRange(_db.Comments.Where(c => c.ParentCommentId == commentId));
        _db.Comments.Remove(comment);
        post.CommentCount = Math.Max(0, post.CommentCount - 1 - replyCount);
        await _db.SaveChangesAsync(ct);

        await _commentCounters.DecrementAsync(post.Id, 1 + replyCount, ct);
    }
}
