using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using deblog.Server.Common.Data;
using deblog.Server.Common.Services;
using deblog.Server.Features.Settings;
using deblog.Server.Features.Users;
using deblog.Server.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace server.Tests.Features.Settings;

public class SettingsEndpointsTests : IClassFixture<SettingsTestFactory>
{
    private readonly SettingsTestFactory _factory;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _anonymousClient;

    public SettingsEndpointsTests(SettingsTestFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
        _anonymousClient = factory.CreateAnonymousClient();

        // Seed initial admin profile in the test DbContext
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Users.Any(u => u.Role == UserRoles.Admin))
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = TestWebApplicationFactory.DefaultAdminEmail,
                Username = "admin",
                DisplayName = "Derick James",
                Bio = "Author Bio",
                Role = UserRoles.Admin,
                Status = UserStatus.Active,
                Information = new UserInformation
                {
                    Id = Guid.NewGuid(),
                    JobTitle = "Tech Lead",
                    Tagline = "Code and Design",
                    Location = "Manila",
                    BannerUrl = "https://res.cloudinary.com/test-cloud/image/upload/v1/default-banner.webp",
                    CopyrightYear = "2026"
                }
            };
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }

    private static string GetImagePath(string filename)
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "web", "public", "assets", "images")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not find repository root containing web/public/assets/images.");
        }

        return Path.Combine(dir.FullName, "web", "public", "assets", "images", filename);
    }

    [Fact]
    public async Task GetSettings_ReturnsOkWithSiteSettings()
    {
        var response = await _anonymousClient.GetAsync("/api/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var settings = await response.Content.ReadFromJsonAsync<SiteSettingsResponse>();
        Assert.NotNull(settings);
        Assert.False(string.IsNullOrWhiteSpace(settings.Email));
        Assert.False(string.IsNullOrWhiteSpace(settings.DisplayName));
        Assert.True(settings.CloudinaryConfigured);
    }

    [Fact]
    public async Task UpdateSettings_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new UpdateSiteSettingsRequest(
            DisplayName: "Hacker",
            Role: "Attacker",
            Tagline: null,
            Bio: null,
            Location: null,
            AvatarUrl: null,
            BannerUrl: null,
            CopyrightYear: null,
            SocialLinksJson: null
        );

        var response = await _anonymousClient.PutAsJsonAsync("/api/settings", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_WithBase64Image_ReturnsBadRequest()
    {
        var request = new UpdateSiteSettingsRequest(
            DisplayName: "Derick James",
            Role: "Developer",
            Tagline: "Test",
            Bio: "Test Bio",
            Location: "Manila",
            AvatarUrl: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
            BannerUrl: null,
            CopyrightYear: "2026",
            SocialLinksJson: null
        );

        var response = await _adminClient.PutAsJsonAsync("/api/settings", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Base64", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateSettings_WithValidCloudinaryUrls_UpdatesSuccessfully()
    {
        var request = new UpdateSiteSettingsRequest(
            DisplayName: "Derick James Espinosa",
            Role: "Senior Full Stack Engineer",
            Tagline: "Building high-performance APIs and clean web applications.",
            Bio: "Full-stack engineer passionate about cloud architecture and developer tooling.",
            Location: "Philippines",
            AvatarUrl: "https://res.cloudinary.com/test-cloud/image/upload/v1/deblog/avatars/me.webp",
            BannerUrl: "https://res.cloudinary.com/test-cloud/image/upload/v1/deblog/banners/banner.webp",
            CopyrightYear: "2026",
            SocialLinksJson: "{\"github\":\"https://github.com/deJames-13\"}"
        );

        var response = await _adminClient.PutAsJsonAsync("/api/settings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<SiteSettingsResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Derick James Espinosa", updated.DisplayName);
        Assert.Equal("Senior Full Stack Engineer", updated.Role);
        Assert.Equal("https://res.cloudinary.com/test-cloud/image/upload/v1/deblog/avatars/me.webp", updated.AvatarUrl);
        Assert.Equal("https://res.cloudinary.com/test-cloud/image/upload/v1/deblog/banners/banner.webp", updated.BannerUrl);
    }

    [Fact]
    public async Task UploadAvatar_WithoutAuth_ReturnsUnauthorized()
    {
        using var content = new MultipartFormDataContent();
        var bytes = new byte[100];
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "test.png");

        var response = await _anonymousClient.PostAsync("/api/settings/avatar", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_WithOversizedFile_Returns413PayloadTooLarge()
    {
        using var content = new MultipartFormDataContent();
        var oversizedBytes = new byte[1024 * 1024 + 50];
        oversizedBytes[0] = 0x89;
        oversizedBytes[1] = 0x50;
        oversizedBytes[2] = 0x4E;
        oversizedBytes[3] = 0x47;

        var fileContent = new ByteArrayContent(oversizedBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "giant_avatar.png");

        var response = await _adminClient.PostAsync("/api/settings/avatar", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_WithRealImage_MePng_SavesUrlInDatabase()
    {
        var imagePath = GetImagePath("me.png");
        Assert.True(File.Exists(imagePath));

        using var content = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(imagePath);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "me.png");

        var response = await _adminClient.PostAsync("/api/settings/avatar", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadSettingAssetResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("https://res.cloudinary.com/", result.Url);

        // Verify only URL was stored in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await db.Users.FirstAsync(u => u.Role == UserRoles.Admin);
        Assert.Equal(result.Url, author.AvatarUrl);
        Assert.DoesNotContain("data:image", author.AvatarUrl);
    }

    [Fact]
    public async Task UploadBanner_WithRealImage_LogoPng_SavesUrlInDatabase()
    {
        var imagePath = GetImagePath("logo.png");
        Assert.True(File.Exists(imagePath));

        using var content = new MultipartFormDataContent();
        var bytes = await File.ReadAllBytesAsync(imagePath);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "file", "logo.png");

        var response = await _adminClient.PostAsync("/api/settings/banner", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadSettingAssetResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("https://res.cloudinary.com/", result.Url);

        // Verify only URL was stored in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var author = await db.Users.Include(u => u.Information).FirstAsync(u => u.Role == UserRoles.Admin);
        Assert.NotNull(author.Information);
        Assert.Equal(result.Url, author.Information.BannerUrl);
        Assert.DoesNotContain("data:image", author.Information.BannerUrl);
    }
}

public class SettingsTestFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var cloudinaryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICloudinaryService));
            if (cloudinaryDescriptor != null) services.Remove(cloudinaryDescriptor);

            services.AddSingleton<ICloudinaryService, MockCloudinaryService>();
        });
    }

    public class MockCloudinaryService : ICloudinaryService
    {
        public bool IsConfigured => true;

        public Task<CloudinaryUploadResult> UploadImageAsync(
            Stream stream,
            string fileName,
            string contentType,
            string? folder = "deblog",
            CancellationToken ct = default)
        {
            if (stream.Length > 1024 * 1024)
            {
                throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Exceeds 1MB", 413);
            }

            var safeName = Path.GetFileNameWithoutExtension(fileName);
            var publicId = $"{folder}/{safeName}_{Guid.NewGuid():N}";
            var url = $"https://res.cloudinary.com/test-cloud/image/upload/v1726000000/{publicId}.webp";

            return Task.FromResult(new CloudinaryUploadResult(
                PublicId: publicId,
                SecureUrl: url,
                Format: "webp",
                Width: 800,
                Height: 600,
                Bytes: stream.Length
            ));
        }

        public Task<bool> DeleteImageAsync(string publicId, CancellationToken ct = default)
        {
            return Task.FromResult(true);
        }
    }
}
