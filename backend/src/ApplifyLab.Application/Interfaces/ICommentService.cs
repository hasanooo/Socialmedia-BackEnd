using ApplifyLab.Application.DTOs;

namespace ApplifyLab.Application.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateAsync(Guid authorId, Guid postId, CreateCommentRequest request, CancellationToken ct);
    Task<CommentPageDto> GetForPostAsync(Guid postId, Guid? currentUserId, string? cursor, int limit, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid commentId, CancellationToken ct);
}
