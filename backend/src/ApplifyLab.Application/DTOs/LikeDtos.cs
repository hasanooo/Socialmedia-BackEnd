using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.DTOs;

public record ToggleLikeRequest(LikeableType LikeableType, Guid LikeableId);

public record ToggleLikeResult(bool Liked, long LikeCount);

public record LikerDto(Guid UserId, string FullName, DateTimeOffset LikedAt);

public record LikersPageDto(IReadOnlyList<LikerDto> Items, string? NextCursor);
