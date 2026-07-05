using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.DTOs;

public record PostAuthorDto(Guid Id, string FullName);

public record PostDto(
    Guid Id,
    PostAuthorDto Author,
    string Content,
    string? ImageUrl,
    string? ThumbnailUrl,
    PostVisibility Visibility,
    long LikeCount,
    long CommentCount,
    bool LikedByCurrentUser,
    DateTimeOffset CreatedAt);

public record FeedPageDto(IReadOnlyList<PostDto> Items, string? NextCursor);

public record CreatePostRequest(string Content, PostVisibility Visibility);
