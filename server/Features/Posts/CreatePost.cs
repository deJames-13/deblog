using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using deblog.Server.Features.Media;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class CreatePostEndpoint
{
    public static RouteGroupBuilder MapCreatePost(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            HttpContext httpContext,
            ClaimsPrincipal user,
            ICloudinaryService cloudinary,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            var userEmail = user.GetUserEmail();

            CreatePostRequest request;
            string? uploadedCoverUrl = null;

            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync(ct);
                var title = form["title"].ToString();
                var slugStr = form["slug"].ToString();
                var summary = form["summary"].ToString();
                var content = form["content"].ToString();
                var category = form["category"].ToString();
                var tagsRaw = form["tags"].ToString();
                var statusStr = form["status"].ToString();
                var isFeatured = bool.TryParse(form["isFeatured"], out var f) && f;
                var coverUrl = form["coverImageUrl"].ToString();

                var status = Enum.TryParse<PostStatus>(statusStr, true, out var parsed)
                    ? parsed
                    : PostStatus.Draft;

                var tags = tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                // Check for cover image file attachment
                var file = form.Files["coverImage"] ?? form.Files["file"] ?? (form.Files.Count > 0 ? form.Files[0] : null);
                if (file != null && file.Length > 0)
                {
                    if (file.Length > CloudinaryService.MaxFileSizeBytes)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status413PayloadTooLarge,
                            title: "Payload Too Large",
                            detail: $"Cover image ({file.Length / 1024} KB) exceeds the maximum allowed limit of 1MB."
                        );
                    }

                    if (cloudinary.IsConfigured)
                    {
                        await using var stream = file.OpenReadStream();
                        var uploadResult = await cloudinary.UploadImageAsync(
                            stream,
                            file.FileName,
                            file.ContentType,
                            "deblog/covers",
                            ct
                        );
                        uploadedCoverUrl = uploadResult.SecureUrl;

                        // Also record in media library
                        db.MediaItems.Add(new MediaItem
                        {
                            Id = Guid.NewGuid(),
                            PublicId = uploadResult.PublicId,
                            SecureUrl = uploadResult.SecureUrl,
                            Filename = file.FileName,
                            MimeType = file.ContentType,
                            FileSizeBytes = uploadResult.Bytes > 0 ? uploadResult.Bytes : file.Length,
                            Width = uploadResult.Width,
                            Height = uploadResult.Height,
                            AltText = title,
                            UploadedById = userId
                        });
                    }
                }

                request = new CreatePostRequest(
                    Title: title,
                    Slug: string.IsNullOrWhiteSpace(slugStr) ? null : slugStr,
                    Summary: string.IsNullOrWhiteSpace(summary) ? null : summary,
                    Content: content,
                    Status: status,
                    IsPublished: status == PostStatus.Published,
                    CoverImageUrl: uploadedCoverUrl ?? (string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl),
                    Category: string.IsNullOrWhiteSpace(category) ? null : category,
                    Tags: tags,
                    IsFeatured: isFeatured
                );
            }
            else
            {
                var bodyRequest = await httpContext.Request.ReadFromJsonAsync<CreatePostRequest>(ct);
                if (bodyRequest == null)
                {
                    return Results.BadRequest(new { message = "Invalid JSON payload." });
                }
                request = bodyRequest;
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { message = "Title is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "Content is required" });
            }

            // Find or associate the Author in a single efficient query
            var normalizedEmail = userEmail?.Trim().ToLowerInvariant();
            var author = await db.Users
                .Where(u => (userId.HasValue && u.Id == userId.Value) ||
                            (normalizedEmail != null && u.Email.ToLower() == normalizedEmail) ||
                            u.Role == UserRoles.Admin)
                .OrderByDescending(u => userId.HasValue && u.Id == userId.Value)
                .ThenByDescending(u => normalizedEmail != null && u.Email.ToLower() == normalizedEmail)
                .FirstOrDefaultAsync(ct);

            if (author == null)
            {
                return Results.BadRequest(new { message = "Author profile not found. Ensure main author is seeded." });
            }

            var baseSlug = !string.IsNullOrWhiteSpace(request.Slug)
                ? PostHelpers.GenerateSlug(request.Slug)
                : PostHelpers.GenerateSlug(request.Title);

            var existingSlugs = await db.Posts
                .Where(p => p.Slug == baseSlug || p.Slug.StartsWith(baseSlug + "-"))
                .Select(p => p.Slug)
                .ToListAsync(ct);

            var slug = baseSlug;
            if (existingSlugs.Contains(baseSlug))
            {
                var suffix = 1;
                while (existingSlugs.Contains($"{baseSlug}-{suffix}"))
                {
                    suffix++;
                }
                slug = $"{baseSlug}-{suffix}";
            }

            // Determine status (support both Status and legacy IsPublished flag)
            var statusVal = request.Status;
            if (request.IsPublished.HasValue)
            {
                statusVal = request.IsPublished.Value ? PostStatus.Published : PostStatus.Draft;
            }

            var now = DateTime.UtcNow;
            var post = new Post
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Slug = slug,
                Summary = request.Summary?.Trim(),
                Content = request.Content,
                Status = statusVal,
                PublishedAt = statusVal == PostStatus.Published ? now : null,
                CoverImageUrl = request.CoverImageUrl?.Trim(),
                Category = request.Category?.Trim(),
                Tags = request.Tags ?? [],
                IsFeatured = request.IsFeatured,
                AuthorId = author.Id,
                Author = author,
                IsDeleted = false,
                Analytics = new PostAnalytics
                {
                    Views = 0,
                    Likes = 0,
                    Shares = 0
                }
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Created($"/api/posts/{post.Slug}", PostHelpers.ToDetailDto(post, baseUrl));
        })
        .DisableAntiforgery()
        .RequireAuthorization("AdminOnly")
        .WithName("CreatePost")
        .WithSummary("Create a new blog post as Draft or Published with optional multipart cover image (Admin Only)");

        return group;
    }
}
