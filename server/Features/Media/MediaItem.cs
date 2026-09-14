using deblog.Server.Common.Entities;
using deblog.Server.Features.Users;

namespace deblog.Server.Features.Media;

public class MediaItem : BaseEntity
{
    public string PublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? AltText { get; set; }

    public Guid? UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}
