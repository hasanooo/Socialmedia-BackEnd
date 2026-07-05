namespace ApplifyLab.Application.DTOs;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserDto(Guid Id, string FirstName, string LastName, string Email, DateTimeOffset CreatedAt)
{
    public string FullName => $"{FirstName} {LastName}";
}

public record AuthResult(UserDto User, string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
