namespace ApplifyLab.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Jti { get; }
    DateTimeOffset? TokenExpiresAt { get; }
    bool IsAuthenticated { get; }
}
