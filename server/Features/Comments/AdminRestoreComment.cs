using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class AdminRestoreCommentEndpoint
{
    public static RouteGroupBuilder MapAdminRestoreComment(this RouteGroupBuilder adminCommentsGroup)
    {
        adminCommentsGroup.MapPost("/{id:guid}/restore", async (
            Guid id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var comment = await db.Comments
                .Include(c => c.Post)
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted, ct);

            if (comment == null)
            {
                return Results.NotFound(new { message = "Comment not found in trash" });
            }

            comment.IsDeleted = false;
            comment.DeletedAt = null;
            await db.SaveChangesAsync(ct);

            return Results.Ok(CommentHelpers.ToAdminResponseDto(comment));
        })
        .WithName("AdminRestoreComment")
        .WithSummary("Restore a soft-deleted comment from trash (Admin Only)");

        return adminCommentsGroup;
    }
}
