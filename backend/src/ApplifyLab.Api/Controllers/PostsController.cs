using ApplifyLab.Application.DTOs;
using ApplifyLab.Application.Interfaces;
using ApplifyLab.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApplifyLab.Api.Controllers;

public class CreatePostForm
{
    public string Content { get; set; } = default!;
    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public IFormFile? Image { get; set; }
}

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly IPostService _posts;
    private readonly ICurrentUserService _currentUser;

    public PostsController(IPostService posts, ICurrentUserService currentUser)
    {
        _posts = posts;
        _currentUser = currentUser;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<FeedPageDto>> GetFeed([FromQuery] string? cursor, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var page = await _posts.GetFeedAsync(_currentUser.UserId, cursor, limit, ct);
        return Ok(page);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PostDto>> GetById(Guid id, CancellationToken ct)
    {
        var post = await _posts.GetByIdAsync(id, _currentUser.UserId, ct);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    [RequestSizeLimit(MaxImageBytes + 1024 * 10)]
    public async Task<ActionResult<PostDto>> Create([FromForm] CreatePostForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Content))
            return BadRequest(new { error = "Content is required." });
        if (form.Content.Length > 5000)
            return BadRequest(new { error = "Content is too long." });

        Stream? imageStream = null;
        string? fileName = null;
        string? contentType = null;

        if (form.Image is not null)
        {
            if (form.Image.Length > MaxImageBytes)
                return BadRequest(new { error = "Image exceeds the 5MB limit." });
            if (!AllowedImageTypes.Contains(form.Image.ContentType))
                return BadRequest(new { error = "Unsupported image type. Use JPEG, PNG or WebP." });

            var ext = Path.GetExtension(form.Image.FileName);
            if (!string.IsNullOrEmpty(ext) && !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext.ToLowerInvariant()))
                return BadRequest(new { error = "Unsupported file extension." });

            imageStream = form.Image.OpenReadStream();
            fileName = form.Image.FileName;
            contentType = form.Image.ContentType;
        }

        var userId = _currentUser.UserId!.Value;
        var post = await _posts.CreateAsync(userId, new CreatePostRequest(form.Content, form.Visibility), imageStream, fileName, contentType, ct);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _posts.DeleteAsync(_currentUser.UserId!.Value, id, ct);
        return NoContent();
    }
}
