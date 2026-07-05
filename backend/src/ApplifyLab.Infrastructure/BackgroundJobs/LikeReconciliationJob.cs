using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Enums;
using ApplifyLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApplifyLab.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring job (see Program.cs RecurringJob registration) that corrects drift between the
/// Redis like counters/sets and Postgres — e.g. after a Redis eviction or a crashed job.
/// Bounded to the most recently active rows per run rather than a full table scan, since a
/// full reconciliation over millions of rows every run would defeat the point of caching.
/// </summary>
public class LikeReconciliationJob : ILikeReconciliationJob
{
    private const int BatchSize = 200;

    private readonly AppDbContext _db;
    private readonly ILikeCounterCache _likeCounters;

    public LikeReconciliationJob(AppDbContext db, ILikeCounterCache likeCounters)
    {
        _db = db;
        _likeCounters = likeCounters;
    }

    public async Task ReconcileAsync(CancellationToken ct)
    {
        var recentPostIds = await _db.Posts
            .OrderByDescending(p => p.CreatedAt)
            .Take(BatchSize)
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var postId in recentPostIds)
        {
            var actual = await _db.Likes.CountAsync(l => l.LikeableType == LikeableType.Post && l.LikeableId == postId, ct);
            var post = await _db.Posts.FirstAsync(p => p.Id == postId, ct);
            if (post.LikeCount != actual)
            {
                post.LikeCount = actual;
            }
        }
        await _db.SaveChangesAsync(ct);

        var recentCommentIds = await _db.Comments
            .OrderByDescending(c => c.CreatedAt)
            .Take(BatchSize)
            .Select(c => c.Id)
            .ToListAsync(ct);

        foreach (var commentId in recentCommentIds)
        {
            var actual = await _db.Likes.CountAsync(l => l.LikeableType == LikeableType.Comment && l.LikeableId == commentId, ct);
            var comment = await _db.Comments.FirstAsync(c => c.Id == commentId, ct);
            if (comment.LikeCount != actual)
            {
                comment.LikeCount = actual;
            }
        }
        await _db.SaveChangesAsync(ct);
    }
}
