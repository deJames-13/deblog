using System.ComponentModel.DataAnnotations;
using deblog.Server.Common.Data;
using deblog.Server.Features.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class CreateGuestCommentEndpoint
{
    public static RouteGroupBuilder MapCreateGuestComment(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
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

            var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var author = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

            if (author != null)
            {
                if (author.IsDeleted || author.Status == UserStatus.Suspended || author.Status == UserStatus.Banned)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Forbidden",
                        detail: "Users who are suspended, banned, or deactivated cannot post comments."
                    );
                }
            }

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
                    Role = UserRoles.Guest,
                    Status = UserStatus.Active,
                    IsDeleted = false
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
                Author = author,
                Status = CommentStatus.Pending,
                ManagementToken = managementToken,
                IsGuest = true,
                IsDeleted = false
            };

            db.Comments.Add(comment);

            // Increment daily telemetry comments count
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var telemetry = await db.DailyTelemetries.FirstOrDefaultAsync(t => t.Date == today, ct);
            if (telemetry == null)
            {
                telemetry = new deblog.Server.Features.Analytics.DailyTelemetry
                {
                    Id = Guid.NewGuid(),
                    Date = today
                };
                db.DailyTelemetries.Add(telemetry);
            }
            telemetry.CommentsCount++;

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

        return group;
    }
}
