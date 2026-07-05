namespace ApplifyLab.Application.Interfaces;

public record UploadedFile(string Key, string Url, string ContentType, long SizeBytes);

public interface IFileStorageService
{
    Task<UploadedFile> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct);
    Task<UploadedFile> UploadBytesAsync(byte[] content, string key, string contentType, CancellationToken ct);
    Task<Stream> OpenReadAsync(string key, CancellationToken ct);
    string GetPublicUrl(string key);
}
