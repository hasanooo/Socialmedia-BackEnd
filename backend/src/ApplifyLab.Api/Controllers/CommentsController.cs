using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApplifyLab.Api.Controllers;

[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;
    private readonly ICurrentUserService _currentUser;

    public CommentsController(ICommentService comments, ICurrentUserService currentUser)
    {
        _comments = comments;
        _currentUser = currentUser;
    }

    [HttpGet("api/posts/{postId:guid}/comments")]
    [AllowAnonymous]
    public async Task<ActionResult<CommentPageDto>> GetForPost(Guid postId, [FromQuery] string? cursor, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var page = await _comments.GetForPostAsync(postId, _currentUser.UserId, cursor, limit, ct);
        return Ok(page);
    }

    [HttpPost("api/posts/{postId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> Create(Guid postId, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required." });

        var comment = await _comments.CreateAsync(_currentUser.UserId!.Value, postId, request, ct);
        return Ok(comment);
    }

    [HttpDelete("api/comments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _comments.DeleteAsync(_currentUser.UserId!.Value, id, ct);
        return NoContent();
    }
}
