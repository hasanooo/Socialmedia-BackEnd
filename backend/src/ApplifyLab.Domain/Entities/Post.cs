using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    /// <summary>DB identity bigint used as the cursor pagination tie-breaker (see Cursor.cs).</summary>
    public long SequenceNumber { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public PostVisibility Visibility { get; set; }
    public long LikeCount { get; set; }
    public long CommentCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
