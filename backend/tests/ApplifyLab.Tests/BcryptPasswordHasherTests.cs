using ApplifyLab.Infrastructure.Auth;

namespace ApplifyLab.Tests;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesADifferentStringThanThePlaintext()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        Assert.NotEqual("Sup3rSecret!", hash);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForTheCorrectPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        Assert.True(_hasher.Verify("Sup3rSecret!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTheWrongPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");
        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_ProducesADifferentHashEachTime_DueToRandomSalt()
    {
        var hash1 = _hasher.Hash("Sup3rSecret!");
        var hash2 = _hasher.Hash("Sup3rSecret!");
        Assert.NotEqual(hash1, hash2);
    }
}
