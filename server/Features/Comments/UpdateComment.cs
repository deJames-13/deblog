using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class UpdateCommentEndpoint
{
    public static RouteGroupBuilder MapUpdateComment(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (
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
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

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
            if (!isAdmin)
            {
                comment.Status = CommentStatus.Pending;
            }

            await db.SaveChangesAsync(ct);

            return Results.Ok(CommentHelpers.ToResponseDto(comment));
        })
        .WithName("UpdateComment")
        .WithSummary("Update a comment (requires guest X-Comment-Token header or Admin auth)");

        return group;
    }
}
