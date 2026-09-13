using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class AdminForceDeleteCommentEndpoint
{
    public static RouteGroupBuilder MapAdminForceDeleteComment(this RouteGroupBuilder adminCommentsGroup)
    {
        adminCommentsGroup.MapDelete("/{id:guid}/force", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (comment == null)
            {
                return Results.NotFound(new { message = "Comment not found" });
            }

            db.Comments.Remove(comment);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("AdminForceDeleteComment")
        .WithSummary("Permanently delete a comment from database (Admin Only)");

        return adminCommentsGroup;
    }
}
