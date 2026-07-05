namespace ApplifyLab.Application.DTOs;

public record CommentDto(
    Guid Id,
    Guid PostId,
    PostAuthorDto Author,
    Guid? ParentCommentId,
    string Content,
    long LikeCount,
    bool LikedByCurrentUser,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CommentDto> Replies);

public record CommentPageDto(IReadOnlyList<CommentDto> Items, string? NextCursor);

public record CreateCommentRequest(string Content, Guid? ParentCommentId);
