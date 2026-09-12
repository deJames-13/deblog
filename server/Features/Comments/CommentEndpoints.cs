using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using deblog.Server.Features.Posts;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        // ==================== Public & Guest Comment Endpoints ====================

        var postCommentsGroup = app.MapGroup("/api/posts/{postId:guid}/comments")
            .WithTags("Comments");

        // GET /api/posts/{postId:guid}/comments - Public read (Approved only)
        postCommentsGroup.MapGet("/", async (
            Guid postId,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var postExists = await db.Posts.AnyAsync(p => p.Id == postId, ct);
            if (!postExists)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            var comments = await db.Comments
                .AsNoTracking()
                .Where(c => c.PostId == postId && c.Status == CommentStatus.Approved)
                .Include(c => c.Author)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentResponseDto(
                    c.Id,
                    c.PostId,
                    c.Content,
                    c.Status,
                    c.CreatedAt,
                    c.UpdatedAt,
                    new CommentAuthorDto(c.Author.Id, c.Author.Username, c.Author.DisplayName ?? "Anonymous", c.Author.AvatarUrl)
                ))
                .ToListAsync(ct);

            return Results.Ok(comments);
        })
        .WithName("GetApprovedPostComments")
        .WithSummary("Get all approved comments for a specific post");

        // POST /api/posts/{postId:guid}/comments - Guest creation (requires email, creates/finds user, returns management token)
        postCommentsGroup.MapPost("/", async (
            Guid postId,
            [FromBody] CreateGuestCommentRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "Comment content cannot be empty" });
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
            {
                return Results.BadRequest(new { message = "A valid email address is required to post a comment" });
            }

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var author = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

            var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? "Anonymous"
                : request.DisplayName.Trim();

            if (author == null)
            {
                var usernameBase = normalizedEmail.Split('@')[0];
                var uniqueSuffix = Guid.NewGuid().ToString()[..4];
                var username = $"{usernameBase}_{uniqueSuffix}";

                author = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    Username = username,
                    DisplayName = displayName,
                    Role = UserRoles.Guest
                };

                db.Users.Add(author);
            }
            else if (author.DisplayName == "Anonymous" && displayName != "Anonymous")
            {
                author.DisplayName = displayName;
            }

            var managementToken = Guid.NewGuid();
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = request.Content.Trim(),
                PostId = postId,
                AuthorId = author.Id,
                Status = CommentStatus.Pending,
                ManagementToken = managementToken,
                IsGuest = true
            };

            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);

            var responseDto = new CommentCreatedResponseDto(
                comment.Id,
                comment.PostId,
                comment.Content,
                comment.Status,
                comment.ManagementToken,
                comment.CreatedAt,
                new CommentAuthorDto(author.Id, author.Username, author.DisplayName, author.AvatarUrl)
            );

            return Results.Created($"/api/posts/{postId}/comments/{comment.Id}", responseDto);
        })
        .WithName("CreateGuestComment")
        .WithSummary("Post a comment as a guest with email validation (returns management token for editing/removal)");

        // ==================== Comment Management (Guest Token or Admin) ====================

        var commentsGroup = app.MapGroup("/api/comments")
            .WithTags("Comments");

        // PUT /api/comments/{id:guid} - Edit comment (Guest with token OR Admin)
        commentsGroup.MapPut("/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Comment-Token")] Guid? headerToken,
            [FromBody] UpdateCommentRequest request,
            ClaimsPrincipal user,
            IConfiguration config,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (comment == null)
            {
                return Results.NotFound(new { message = "Comment not found" });
            }

            var isAdmin = user.IsAdmin(config);
            var token = headerToken ?? request.ManagementToken;

            if (!isAdmin && (token == null || comment.ManagementToken != token.Value))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "Content cannot be empty" });
            }

            comment.Content = request.Content.Trim();
            // If edited, reset to Pending for re-moderation unless edited by Admin
            if (!isAdmin)
            {
                comment.Status = CommentStatus.Pending;
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new CommentResponseDto(
                comment.Id,
                comment.PostId,
                comment.Content,
                comment.Status,
                comment.CreatedAt,
                comment.UpdatedAt,
                new CommentAuthorDto(comment.Author.Id, comment.Author.Username, comment.Author.DisplayName ?? "Anonymous", comment.Author.AvatarUrl)
            ));
        })
        .WithName("UpdateComment")
        .WithSummary("Update a comment (requires guest X-Comment-Token header or Admin auth)");

        // DELETE /api/comments/{id:guid} - Soft-remove (Guest) or Hard-delete (Admin)
        commentsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Comment-Token")] Guid? headerToken,
            [FromQuery] Guid? token,
            [FromQuery] bool? permanent,
            ClaimsPrincipal user,
            IConfiguration config,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (comment == null)
            {
                return Results.NotFound(new { message = "Comment not found" });
            }

            var effectiveToken = headerToken ?? token;
            var isAdmin = user.IsAdmin(config);

            if (!isAdmin && (effectiveToken == null || comment.ManagementToken != effectiveToken.Value))
            {
                return Results.Unauthorized();
            }

            if (isAdmin && permanent == true)
            {
                // Admin can permanently hard-delete
                db.Comments.Remove(comment);
            }
            else
            {
                // Soft remove for guests or default admin removal
                comment.Status = CommentStatus.Removed;
                comment.Content = "[Comment removed by author]";
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Comment has been removed successfully" });
        })
        .WithName("DeleteComment")
        .WithSummary("Remove a comment (soft-removes for guests with token; can hard-delete for Admin)");

        // ==================== Admin Comment Moderation ====================

        var adminCommentsGroup = app.MapGroup("/api/admin/comments")
            .WithTags("Admin Comments")
            .RequireAuthorization("AdminOnly");

        // GET /api/admin/comments - List comments across posts with status filter
        adminCommentsGroup.MapGet("/", async (
            [FromQuery] CommentStatus? status,
            [FromQuery] Guid? postId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var currentPage = page.GetValueOrDefault(1) <= 0 ? 1 : page.GetValueOrDefault(1);
            var currentLimit = pageSize.GetValueOrDefault(20) <= 0 ? 20 : Math.Clamp(pageSize.GetValueOrDefault(20), 1, 100);

            var query = db.Comments
                .AsNoTracking()
                .Include(c => c.Post)
                .Include(c => c.Author)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (postId.HasValue)
            {
                query = query.Where(c => c.PostId == postId.Value);
            }

            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)currentLimit);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((currentPage - 1) * currentLimit)
                .Take(currentLimit)
                .Select(c => new AdminCommentResponseDto(
                    c.Id,
                    c.PostId,
                    c.Post.Title,
                    c.Content,
                    c.Status,
                    c.IsGuest,
                    c.CreatedAt,
                    c.UpdatedAt,
                    new CommentAuthorDto(c.Author.Id, c.Author.Username, c.Author.DisplayName ?? "Anonymous", c.Author.AvatarUrl)
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<AdminCommentResponseDto>(items, currentPage, currentLimit, totalCount, totalPages));
        })
        .WithName("AdminListComments")
        .WithSummary("List all comments for moderation (Admin Only)");

        // PATCH /api/admin/comments/{id:guid}/status - Moderate comment status
        adminCommentsGroup.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateCommentStatusRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (comment == null)
            {
                return Results.NotFound(new { message = "Comment not found" });
            }

            comment.Status = request.Status;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = $"Comment status updated to {request.Status}", id = comment.Id, status = comment.Status });
        })
        .WithName("AdminUpdateCommentStatus")
        .WithSummary("Approve, reject, or mark comment as pending (Admin Only)");

        return app;
    }
}
