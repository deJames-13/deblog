using System.Net;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using deblog.Server.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace server.Tests.Common;

public class CloudinaryServiceTests
{
    private static string GetImagePath(string filename)
    {
        var baseDir = AppContext.BaseDirectory;
        // Search upwards for web/public/assets/images
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "web", "public", "assets", "images")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not find repository root containing web/public/assets/images.");
        }

        var fullPath = Path.Combine(dir.FullName, "web", "public", "assets", "images", filename);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Test image not found at {fullPath}");
        }

        return fullPath;
    }

    private static IConfiguration CreateConfig(string? cloudinaryUrl = null)
    {
        var dict = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            dict["CLOUDINARY_URL"] = cloudinaryUrl;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private static Cloudinary CreateMockCloudinary(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var account = new Account("test-cloud", "1234567890", "test-secret");
        var testHandler = new TestHttpMessageHandler(handlerFunc);
        var customClient = new HttpClient(testHandler);

        var cloudinary = new Cloudinary(account);
        cloudinary.Api.Client = customClient;
        return cloudinary;
    }

    [Fact]
    public void IsConfigured_WhenNoCredentials_ReturnsFalse()
    {
        var service = new CloudinaryService(CreateConfig(null), NullLogger<CloudinaryService>.Instance);
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public async Task UploadImageAsync_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        var service = new CloudinaryService(CreateConfig(null), NullLogger<CloudinaryService>.Instance);
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadImageAsync(stream, "test.png", "image/png"));

        Assert.Contains("Cloudinary is not configured", ex.Message);
    }

    [Fact]
    public async Task UploadImageAsync_WithUnsupportedMimeType_ThrowsBadHttpRequestException400()
    {
        var config = CreateConfig("cloudinary://12345:secret@test-cloud");
        var service = new CloudinaryService(config, NullLogger<CloudinaryService>.Instance);
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.UploadImageAsync(stream, "document.pdf", "application/pdf"));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Contains("Unsupported image content-type", ex.Message);
    }

    [Fact]
    public async Task UploadImageAsync_WithEmptyStream_ThrowsBadHttpRequestException400()
    {
        var config = CreateConfig("cloudinary://12345:secret@test-cloud");
        var service = new CloudinaryService(config, NullLogger<CloudinaryService>.Instance);
        using var stream = new MemoryStream();

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.UploadImageAsync(stream, "empty.png", "image/png"));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadImageAsync_WithOversizedFile_ThrowsBadHttpRequestException413()
    {
        var config = CreateConfig("cloudinary://12345:secret@test-cloud");
        var service = new CloudinaryService(config, NullLogger<CloudinaryService>.Instance);

        // 1MB + 100 bytes
        var oversizedBytes = new byte[1024 * 1024 + 100];
        // PNG header
        oversizedBytes[0] = 0x89;
        oversizedBytes[1] = 0x50;
        oversizedBytes[2] = 0x4E;
        oversizedBytes[3] = 0x47;

        using var stream = new MemoryStream(oversizedBytes);

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.UploadImageAsync(stream, "too-big.png", "image/png"));

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
        Assert.Contains("exceeds the maximum allowed limit", ex.Message);
    }

    [Fact]
    public async Task UploadImageAsync_WithFakeExtension_FailsMagicBytesCheck400()
    {
        var config = CreateConfig("cloudinary://12345:secret@test-cloud");
        var service = new CloudinaryService(config, NullLogger<CloudinaryService>.Instance);

        // Text file content disguised with image/png MIME type
        var textBytes = Encoding.UTF8.GetBytes("This is plain text disguised as an image file.");
        using var stream = new MemoryStream(textBytes);

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.UploadImageAsync(stream, "malicious.png", "image/png"));

        Assert.Equal(StatusCodes.Status400BadRequest, ex.StatusCode);
        Assert.Contains("magic bytes check failed", ex.Message);
    }

    [Fact]
    public async Task UploadImageAsync_WithRealImage_MePng_UploadsSuccessfully()
    {
        var imagePath = GetImagePath("me.png");
        Assert.True(File.Exists(imagePath), "me.png must exist in web/public/assets/images");

        var fileInfo = new FileInfo(imagePath);
        Assert.True(fileInfo.Length <= 1024 * 1024, $"me.png ({fileInfo.Length} bytes) must be <= 1MB");

        var mockCloudinary = CreateMockCloudinary(req =>
        {
            var json = """
            {
                "public_id": "deblog/avatars/me_avatar_123",
                "version": 1726000000,
                "signature": "abcdef123456",
                "width": 500,
                "height": 500,
                "format": "webp",
                "resource_type": "image",
                "created_at": "2026-09-14T08:00:00Z",
                "bytes": 35000,
                "type": "upload",
                "url": "http://res.cloudinary.com/test-cloud/image/upload/v1726000000/deblog/avatars/me_avatar_123.webp",
                "secure_url": "https://res.cloudinary.com/test-cloud/image/upload/v1726000000/deblog/avatars/me_avatar_123.webp"
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new CloudinaryService(mockCloudinary, NullLogger<CloudinaryService>.Instance);

        await using var stream = File.OpenRead(imagePath);
        var result = await service.UploadImageAsync(stream, "me.png", "image/png", folder: "deblog/avatars");

        Assert.NotNull(result);
        Assert.Equal("deblog/avatars/me_avatar_123", result.PublicId);
        Assert.Equal("https://res.cloudinary.com/test-cloud/image/upload/v1726000000/deblog/avatars/me_avatar_123.webp", result.SecureUrl);
        Assert.Equal("webp", result.Format);
        Assert.Equal(500, result.Width);
        Assert.Equal(500, result.Height);
    }

    [Fact]
    public async Task UploadImageAsync_WithRealImage_LogoPng_UploadsSuccessfully()
    {
        var imagePath = GetImagePath("logo.png");
        Assert.True(File.Exists(imagePath), "logo.png must exist in web/public/assets/images");

        var fileInfo = new FileInfo(imagePath);
        Assert.True(fileInfo.Length <= 1024 * 1024, $"logo.png ({fileInfo.Length} bytes) must be <= 1MB");

        var mockCloudinary = CreateMockCloudinary(req =>
        {
            var json = """
            {
                "public_id": "deblog/banners/logo_banner_456",
                "version": 1726000001,
                "signature": "abcdef987654",
                "width": 1200,
                "height": 400,
                "format": "webp",
                "resource_type": "image",
                "created_at": "2026-09-14T08:00:00Z",
                "bytes": 22000,
                "type": "upload",
                "url": "http://res.cloudinary.com/test-cloud/image/upload/v1726000001/deblog/banners/logo_banner_456.webp",
                "secure_url": "https://res.cloudinary.com/test-cloud/image/upload/v1726000001/deblog/banners/logo_banner_456.webp"
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new CloudinaryService(mockCloudinary, NullLogger<CloudinaryService>.Instance);

        await using var stream = File.OpenRead(imagePath);
        var result = await service.UploadImageAsync(stream, "logo.png", "image/png", folder: "deblog/banners");

        Assert.NotNull(result);
        Assert.Equal("deblog/banners/logo_banner_456", result.PublicId);
        Assert.Equal("https://res.cloudinary.com/test-cloud/image/upload/v1726000001/deblog/banners/logo_banner_456.webp", result.SecureUrl);
        Assert.Equal("webp", result.Format);
    }

    [Fact]
    public async Task DeleteImageAsync_WhenNotConfigured_ReturnsFalse()
    {
        var service = new CloudinaryService(CreateConfig(null), NullLogger<CloudinaryService>.Instance);
        var result = await service.DeleteImageAsync("test_id");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteImageAsync_WithEmptyPublicId_ReturnsFalse()
    {
        var config = CreateConfig("cloudinary://12345:secret@test-cloud");
        var service = new CloudinaryService(config, NullLogger<CloudinaryService>.Instance);
        var result = await service.DeleteImageAsync("");
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteImageAsync_WithConfiguredCloudinary_ReturnsTrueOnSuccess()
    {
        var mockCloudinary = CreateMockCloudinary(req =>
        {
            var json = """
            {
                "result": "ok"
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var service = new CloudinaryService(mockCloudinary, NullLogger<CloudinaryService>.Instance);
        var result = await service.DeleteImageAsync("deblog/avatars/me_avatar_123");
        Assert.True(result);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
