using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class SoftDeleteCommentEndpoint
{
    public static RouteGroupBuilder MapSoftDeleteComment(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Comment-Token")] Guid? headerToken,
            [FromQuery] Guid? token,
            ClaimsPrincipal user,
            IConfiguration config,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
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

            comment.IsDeleted = true;
            comment.DeletedAt = DateTime.UtcNow;
            comment.Content = "[Comment removed by author]";
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Comment has been removed successfully" });
        })
        .WithName("DeleteComment")
        .WithSummary("Remove a comment (soft-removes for guests with token or Admin)");

        return group;
    }
}
