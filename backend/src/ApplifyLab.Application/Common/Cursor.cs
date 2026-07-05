using System.Text;

namespace ApplifyLab.Application.Common;

/// <summary>
/// Opaque cursor encoding (createdAt ticks, monotonic sequence number) so feed/comment/like
/// pagination never uses OFFSET, which degrades badly once tables reach millions of rows.
/// The sequence number (a DB identity bigint) is used as the tie-breaker instead of the row's
/// Guid id, since Guid ordering doesn't translate reliably to SQL comparisons via EF Core.
/// </summary>
public static class Cursor
{
    public static string Encode(DateTimeOffset createdAt, long sequenceNumber)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{createdAt.UtcTicks}_{sequenceNumber}"));

    public static (DateTimeOffset createdAt, long sequenceNumber)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('_', 2);
            var ticks = long.Parse(parts[0]);
            var sequenceNumber = long.Parse(parts[1]);
            return (new DateTimeOffset(ticks, TimeSpan.Zero), sequenceNumber);
        }
        catch
        {
            return null;
        }
    }
}
