using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace deblog.Server.Common.Services;

public class CloudinaryService : ICloudinaryService
{
    public const long MaxFileSizeBytes = 1024 * 1024; // 1 MB limit
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/avif"
    };

    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public bool IsConfigured => _cloudinary != null;

    public CloudinaryService(Cloudinary? cloudinary, ILogger<CloudinaryService> logger)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var cloudinaryUrl = config["CLOUDINARY_URL"];
        var cloudName = config["CLOUDINARY_CLOUD_NAME"];
        var apiKey = config["CLOUDINARY_API_KEY"];
        var apiSecret = config["CLOUDINARY_API_SECRET"];

        if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
            _cloudinary.Api.Secure = true;
            _logger.LogInformation("Cloudinary initialized via CLOUDINARY_URL.");
        }
        else if (!string.IsNullOrWhiteSpace(cloudName) &&
                 !string.IsNullOrWhiteSpace(apiKey) &&
                 !string.IsNullOrWhiteSpace(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
            _logger.LogInformation("Cloudinary initialized via API credentials for cloud: {CloudName}", cloudName);
        }
        else
        {
            _logger.LogWarning("Cloudinary credentials not configured. Image uploads will operate in offline/degraded mode.");
        }
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? folder = "deblog",
        CancellationToken ct = default)
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException("Cloudinary is not configured on the server. Image operations are offline.");
        }

        // 1. Validate MIME Type
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedMimeTypes.Contains(contentType))
        {
            throw new BadHttpRequestException($"Unsupported image content-type '{contentType}'. Allowed types: JPEG, PNG, WebP, AVIF.", StatusCodes.Status400BadRequest);
        }

        // 2. Read stream into memory to validate size & magic bytes
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);

        if (memoryStream.Length == 0)
        {
            throw new BadHttpRequestException("The uploaded image file is empty.", StatusCodes.Status400BadRequest);
        }

        if (memoryStream.Length > MaxFileSizeBytes)
        {
            throw new BadHttpRequestException($"Image file size ({memoryStream.Length / 1024} KB) exceeds the maximum allowed limit of 1MB (1024 KB).", StatusCodes.Status413PayloadTooLarge);
        }

        // 3. Inspect Magic Bytes
        var buffer = memoryStream.ToArray();
        if (!IsValidImageHeader(buffer))
        {
            throw new BadHttpRequestException("The uploaded file does not match a valid image header (magic bytes check failed).", StatusCodes.Status400BadRequest);
        }

        memoryStream.Position = 0;

        // 4. Execute Cloudinary upload with standard optimization
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, memoryStream),
            Folder = folder ?? "deblog",
            Transformation = new Transformation()
                .Width(2560)
                .Crop("limit")
                .Quality("auto")
                .FetchFormat("auto"),
            Overwrite = false,
            UniqueFilename = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);

        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Message}", uploadResult.Error.Message);
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return new CloudinaryUploadResult(
            PublicId: uploadResult.PublicId,
            SecureUrl: uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
            Format: uploadResult.Format ?? "webp",
            Width: uploadResult.Width,
            Height: uploadResult.Height,
            Bytes: uploadResult.Bytes
        );
    }

    public async Task<bool> DeleteImageAsync(string publicId, CancellationToken ct = default)
    {
        if (_cloudinary == null)
        {
            _logger.LogWarning("Cannot delete image '{PublicId}' - Cloudinary is not configured.", publicId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);

        if (result.Result != "ok" && result.Result != "not found")
        {
            _logger.LogWarning("Cloudinary delete for '{PublicId}' returned: {Result}", publicId, result.Result);
            return false;
        }

        return true;
    }

    private static bool IsValidImageHeader(byte[] header)
    {
        if (header.Length < 4) return false;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;

        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;

        // WebP: RIFF....WEBP
        if (header.Length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;

        // AVIF: ....ftyp (box header)
        if (header.Length >= 12 &&
            header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70) return true;

        // GIF (optional fallback): GIF87a / GIF89a
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;

        return false;
    }
}
