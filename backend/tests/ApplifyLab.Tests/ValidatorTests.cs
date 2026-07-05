using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Validators;
using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Tests;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var result = _validator.Validate(new RegisterRequest("Ada", "Lovelace", "ada@example.com", "Str0ngPass!"));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short1A")] // < 8 chars
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")] // no digit
    public void Weak_Password_Fails(string password)
    {
        var result = _validator.Validate(new RegisterRequest("Ada", "Lovelace", "ada@example.com", password));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.Validate(new RegisterRequest("Ada", "Lovelace", "not-an-email", "Str0ngPass!"));
        Assert.False(result.IsValid);
    }
}

public class CreatePostRequestValidatorTests
{
    private readonly CreatePostRequestValidator _validator = new();

    [Fact]
    public void Empty_Content_Fails()
    {
        var result = _validator.Validate(new CreatePostRequest("", PostVisibility.Public));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Content_Over_5000_Chars_Fails()
    {
        var result = _validator.Validate(new CreatePostRequest(new string('x', 5001), PostVisibility.Public));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_Post_Passes()
    {
        var result = _validator.Validate(new CreatePostRequest("Hello world", PostVisibility.Private));
        Assert.True(result.IsValid);
    }
}
