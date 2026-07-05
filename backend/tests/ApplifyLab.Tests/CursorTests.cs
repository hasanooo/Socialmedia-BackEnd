using ApplifyLab.Application.Common;

namespace ApplifyLab.Tests;

public class CursorTests
{
    [Fact]
    public void EncodeDecode_RoundTrips_ExactValues()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var sequenceNumber = 12345L;

        var cursor = Cursor.Encode(createdAt, sequenceNumber);
        var decoded = Cursor.Decode(cursor);

        Assert.NotNull(decoded);
        Assert.Equal(createdAt.UtcTicks, decoded!.Value.createdAt.UtcTicks);
        Assert.Equal(sequenceNumber, decoded.Value.sequenceNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-valid-base64!!")]
    [InlineData("dGhpcyBpcyBub3QgYSBjdXJzb3I=")] // valid base64, wrong shape
    public void Decode_ReturnsNull_ForInvalidInput(string? input)
    {
        Assert.Null(Cursor.Decode(input));
    }

    [Fact]
    public void Encode_IsOpaqueAndUrlSafeAsBase64()
    {
        var cursor = Cursor.Encode(DateTimeOffset.UtcNow, 1);
        // Should not throw when round-tripped through Convert.FromBase64String.
        var bytes = Convert.FromBase64String(cursor);
        Assert.NotEmpty(bytes);
    }
}
