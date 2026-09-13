using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Comments;

public static class AdminUpdateCommentStatusEndpoint
{
    public static RouteGroupBuilder MapAdminUpdateCommentStatus(this RouteGroupBuilder adminCommentsGroup)
    {
        adminCommentsGroup.MapPatch("/{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateCommentStatusRequest request,
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

            comment.Status = request.Status;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = $"Comment status updated to {request.Status}", id = comment.Id, status = comment.Status });
        })
        .WithName("AdminUpdateCommentStatus")
        .WithSummary("Approve, reject, or mark comment as pending or spam (Admin Only)");

        return adminCommentsGroup;
    }
}
