using ApplifyLab.Domain.Entities;
using ApplifyLab.Infrastructure.Auth;
using ApplifyLab.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ApplifyLab.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(Options.Create(new JwtOptions
    {
        Secret = "test-secret-at-least-32-characters-long!!",
        Issuer = "applifylab-tests",
        Audience = "applifylab-tests-client",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    }));

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com",
        PasswordHash = "irrelevant",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void GenerateAccessToken_ProducesATokenThatValidatesSuccessfully()
    {
        var user = MakeUser();
        var result = _sut.GenerateAccessToken(user);

        var principal = _sut.ValidateAccessToken(result.Token);

        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal!.FindFirst("sub")?.Value);
        Assert.Equal(result.Jti, principal.FindFirst("jti")?.Value);
    }

    [Fact]
    public void ValidateAccessToken_ReturnsNull_ForATamperedToken()
    {
        var user = MakeUser();
        var result = _sut.GenerateAccessToken(user);
        var tampered = result.Token[..^2] + (result.Token[^2] == 'a' ? "bb" : "aa");

        Assert.Null(_sut.ValidateAccessToken(tampered));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueValuesEachCall()
    {
        var a = _sut.GenerateRefreshToken();
        var b = _sut.GenerateRefreshToken();
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var token = _sut.GenerateRefreshToken();
        Assert.Equal(_sut.HashRefreshToken(token), _sut.HashRefreshToken(token));
    }
}
