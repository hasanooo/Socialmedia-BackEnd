using ApplifyLab.Application.DTOs;
using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyLikeChangedAsync(LikeableType type, Guid id, long likeCount, CancellationToken ct);
    Task NotifyCommentAddedAsync(Guid postId, CommentDto comment, CancellationToken ct);
}
