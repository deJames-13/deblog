using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Common.Services;
using deblog.Server.Features.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class UpdatePostEndpoint
{
    public static RouteGroupBuilder MapUpdatePost(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            ClaimsPrincipal user,
            ICloudinaryService cloudinary,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var post = await db.Posts
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            UpdatePostRequest request;
            string? uploadedCoverUrl = null;

            if (httpContext.Request.HasFormContentType)
            {
                var form = await httpContext.Request.ReadFormAsync(ct);
                var title = form.ContainsKey("title") ? form["title"].ToString() : null;
                var slugStr = form.ContainsKey("slug") ? form["slug"].ToString() : null;
                var summary = form.ContainsKey("summary") ? form["summary"].ToString() : null;
                var content = form.ContainsKey("content") ? form["content"].ToString() : null;
                var category = form.ContainsKey("category") ? form["category"].ToString() : null;
                var tagsRaw = form.ContainsKey("tags") ? form["tags"].ToString() : null;
                var coverUrl = form.ContainsKey("coverImageUrl") ? form["coverImageUrl"].ToString() : null;

                PostStatus? status = null;
                if (form.ContainsKey("status") && Enum.TryParse<PostStatus>(form["status"], true, out var parsed))
                {
                    status = parsed;
                }

                bool? isFeatured = form.ContainsKey("isFeatured") && bool.TryParse(form["isFeatured"], out var feat)
                    ? feat
                    : null;

                var tags = tagsRaw != null
                    ? tagsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : null;

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

                        var userId = user.GetUserId();
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
                            AltText = title ?? post.Title,
                            UploadedById = userId
                        });
                    }
                }

                request = new UpdatePostRequest(
                    Title: title,
                    Slug: slugStr,
                    Summary: summary,
                    Content: content,
                    Status: status,
                    IsPublished: status.HasValue ? status.Value == PostStatus.Published : null,
                    CoverImageUrl: uploadedCoverUrl ?? coverUrl,
                    Category: category,
                    Tags: tags,
                    IsFeatured: isFeatured
                );
            }
            else
            {
                var bodyRequest = await httpContext.Request.ReadFromJsonAsync<UpdatePostRequest>(ct);
                if (bodyRequest == null)
                {
                    return Results.BadRequest(new { message = "Invalid JSON payload." });
                }
                request = bodyRequest;
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                post.Title = request.Title.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var newSlug = PostHelpers.GenerateSlug(request.Slug);
                if (newSlug != post.Slug && await db.Posts.AnyAsync(p => p.Slug == newSlug && p.Id != id, ct))
                {
                    return Results.BadRequest(new { message = "Slug is already in use" });
                }
                post.Slug = newSlug;
            }

            if (request.Summary != null)
            {
                post.Summary = request.Summary.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                post.Content = request.Content;
            }

            if (request.CoverImageUrl != null)
            {
                post.CoverImageUrl = request.CoverImageUrl.Trim();
            }

            if (request.Category != null)
            {
                post.Category = request.Category.Trim();
            }

            if (request.Tags != null)
            {
                post.Tags = request.Tags;
            }

            if (request.IsFeatured.HasValue)
            {
                post.IsFeatured = request.IsFeatured.Value;
            }

            if (request.Status.HasValue)
            {
                if (request.Status.Value == PostStatus.Published && post.Status != PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
                post.Status = request.Status.Value;
            }
            else if (request.IsPublished.HasValue)
            {
                var targetStatus = request.IsPublished.Value ? PostStatus.Published : PostStatus.Draft;
                if (targetStatus == PostStatus.Published && post.Status != PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
                post.Status = targetStatus;
            }

            await db.SaveChangesAsync(ct);

            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            return Results.Ok(PostHelpers.ToDetailDto(post, baseUrl));
        })
        .DisableAntiforgery()
        .RequireAuthorization("AdminOnly")
        .WithName("UpdatePost")
        .WithSummary("Update post details, status, or cover image (Admin Only)");

        return group;
    }
}
