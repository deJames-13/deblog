namespace deblog.Server.Common.Services;

public record CloudinaryUploadResult(
    string PublicId,
    string SecureUrl,
    string Format,
    int Width,
    int Height,
    long Bytes
);

public interface ICloudinaryService
{
    bool IsConfigured { get; }
    Task<CloudinaryUploadResult> UploadImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? folder = "deblog",
        CancellationToken ct = default);

    Task<bool> DeleteImageAsync(string publicId, CancellationToken ct = default);
}
