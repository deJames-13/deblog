namespace deblog.Server.Features.Media;

public record MediaItemDto(
    Guid Id,
    string PublicId,
    string Url,
    string Filename,
    string MimeType,
    long FileSizeBytes,
    long FileSizeKb,
    int Width,
    int Height,
    string Dimensions,
    string? AltText,
    DateTime CreatedAt
);

public record UploadMediaResponse(
    MediaItemDto Media,
    string Message
);
