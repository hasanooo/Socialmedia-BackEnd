using ApplifyLab.Application.DTOs;

namespace ApplifyLab.Application.Interfaces;

public interface IPostService
{
    Task<PostDto> CreateAsync(Guid authorId, CreatePostRequest request, Stream? image, string? imageFileName, string? imageContentType, CancellationToken ct);
    Task<FeedPageDto> GetFeedAsync(Guid? currentUserId, string? cursor, int limit, CancellationToken ct);
    Task<PostDto?> GetByIdAsync(Guid postId, Guid? currentUserId, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid postId, CancellationToken ct);
}
