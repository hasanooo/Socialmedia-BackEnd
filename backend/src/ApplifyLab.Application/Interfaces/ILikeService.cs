using ApplifyLab.Application.DTOs;
using ApplifyLab.Domain.Enums;

namespace ApplifyLab.Application.Interfaces;

public interface ILikeService
{
    Task<ToggleLikeResult> ToggleAsync(Guid userId, ToggleLikeRequest request, CancellationToken ct);
    Task<LikersPageDto> GetLikersAsync(LikeableType likeableType, Guid likeableId, string? cursor, int limit, CancellationToken ct);
}
