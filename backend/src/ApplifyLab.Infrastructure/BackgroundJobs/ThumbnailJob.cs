using ApplifyLab.Application.Interfaces;
using ApplifyLab.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ApplifyLab.Infrastructure.BackgroundJobs;

public class ThumbnailJob : IThumbnailJob
{
    private readonly IFileStorageService _fileStorage;
    private readonly AppDbContext _db;

    public ThumbnailJob(IFileStorageService fileStorage, AppDbContext db)
    {
        _fileStorage = fileStorage;
        _db = db;
    }

    public async Task GenerateThumbnailAsync(Guid postId, string sourceKey, CancellationToken ct)
    {
        await using var source = await _fileStorage.OpenReadAsync(sourceKey, ct);

        using var image = await Image.LoadAsync(source, ct);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(400, 400),
        }));

        using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, ct);

        var thumbKey = $"thumbnails/{Path.GetFileNameWithoutExtension(sourceKey)}-thumb.jpg";
        var uploaded = await _fileStorage.UploadBytesAsync(output.ToArray(), thumbKey, "image/jpeg", ct);

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post is not null)
        {
            post.ThumbnailUrl = uploaded.Url;
            await _db.SaveChangesAsync(ct);
        }
    }
}
