using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApplifyLab.Api.Controllers;

[ApiController]
[Route("api/likes")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly ILikeService _likes;
    private readonly ICurrentUserService _currentUser;

    public LikesController(ILikeService likes, ICurrentUserService currentUser)
    {
        _likes = likes;
        _currentUser = currentUser;
    }

    [HttpPost("toggle")]
    public async Task<ActionResult<ToggleLikeResult>> Toggle([FromBody] ToggleLikeRequest request, CancellationToken ct)
    {
        var result = await _likes.ToggleAsync(_currentUser.UserId!.Value, request, ct);
        return Ok(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<LikersPageDto>> GetLikers(
        [FromQuery] LikeableType likeableType,
        [FromQuery] Guid likeableId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var page = await _likes.GetLikersAsync(likeableType, likeableId, cursor, limit, ct);
        return Ok(page);
    }
}
