namespace ApplifyLab.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    /// <summary>DB identity bigint used as the cursor pagination tie-breaker (see Cursor.cs).</summary>
    public long SequenceNumber { get; set; }
    public Guid PostId { get; set; }
    public Post Post { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public string Content { get; set; } = default!;
    public long LikeCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
