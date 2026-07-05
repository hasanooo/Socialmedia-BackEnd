using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Entities;
using ApplifyLab.Domain.Enums;
using ApplifyLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApplifyLab.Infrastructure.BackgroundJobs;

public class LikeSyncJob : ILikeSyncJob
{
    private readonly AppDbContext _db;
    private readonly ILikeCounterCache _likeCounters;

    public LikeSyncJob(AppDbContext db, ILikeCounterCache likeCounters)
    {
        _db = db;
        _likeCounters = likeCounters;
    }

    public async Task SyncLikeAsync(LikeableType type, Guid id, Guid userId, bool liked, CancellationToken ct)
    {
        var existing = await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == userId && l.LikeableType == type && l.LikeableId == id, ct);

        if (liked && existing is null)
        {
            _db.Likes.Add(new Like
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LikeableType = type,
                LikeableId = id,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        else if (!liked && existing is not null)
        {
            _db.Likes.Remove(existing);
        }

        // Redis is the authoritative live counter; mirror it into the denormalized column.
        var count = await _likeCounters.GetCountAsync(type, id, ct);

        if (type == LikeableType.Post)
        {
            var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (post is not null) post.LikeCount = count;
        }
        else
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (comment is not null) comment.LikeCount = count;
        }

        await _db.SaveChangesAsync(ct);
    }
}
