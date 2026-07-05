using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Domain.Entities;

public class Like
{
    public Guid Id { get; set; }
    /// <summary>DB identity bigint used as the cursor pagination tie-breaker (see Cursor.cs).</summary>
    public long SequenceNumber { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public LikeableType LikeableType { get; set; }
    public Guid LikeableId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
