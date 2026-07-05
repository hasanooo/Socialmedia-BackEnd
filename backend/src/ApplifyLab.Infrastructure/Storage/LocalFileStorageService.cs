using ApplifyLab.Application.Interfaces;
using ApplifyLab.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ApplifyLab.Infrastructure.Storage;

/// <summary>
/// Stores uploads on the local filesystem, under the app's content root by default.
/// Files are served back over HTTP via app.UseStaticFiles in Program.cs.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly LocalStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<LocalStorageOptions> options)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.RootPath);
    }

    public async Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct)
    {
        var key = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        await WriteAsync(key, content, ct);
        return new UploadedFile(key, GetPublicUrl(key), contentType, content.Length);
    }

    public async Task<UploadedFile> UploadBytesAsync(byte[] content, string key, string contentType, CancellationToken ct)
    {
        using var stream = new MemoryStream(content);
        await WriteAsync(key, stream, ct);
        return new UploadedFile(key, GetPublicUrl(key), contentType, content.LongLength);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
    {
        Stream stream = File.OpenRead(ResolvePath(key));
        return Task.FromResult(stream);
    }

    public string GetPublicUrl(string key) => $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";

    private async Task WriteAsync(string key, Stream content, CancellationToken ct)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    private string ResolvePath(string key) => Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar));
}
