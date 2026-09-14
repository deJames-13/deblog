using deblog.Server.Common.Data;
using deblog.Server.Features.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Media;

public static class ListMediaEndpoint
{
    public static RouteGroupBuilder MapListMedia(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            [FromQuery] string? query,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var p = page > 0 ? page : 1;
            var ps = pageSize > 0 && pageSize <= 100 ? pageSize : 24;

            var q = db.MediaItems.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lower = query.Trim().ToLower();
                q = q.Where(m => m.Filename.ToLower().Contains(lower) ||
                                (m.AltText != null && m.AltText.ToLower().Contains(lower)));
            }

            var totalCount = await q.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)ps);

            var items = await q
                .OrderByDescending(m => m.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(m => new MediaItemDto(
                    m.Id,
                    m.PublicId,
                    m.SecureUrl,
                    m.Filename,
                    m.MimeType,
                    m.FileSizeBytes,
                    Math.Max(1, m.FileSizeBytes / 1024),
                    m.Width,
                    m.Height,
                    $"{m.Width}x{m.Height}",
                    m.AltText,
                    m.CreatedAt
                ))
                .ToListAsync(ct);

            return Results.Ok(new PagedResult<MediaItemDto>(items, p, ps, totalCount, totalPages));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("ListMedia")
        .WithSummary("List paginated media library items (Admin Only)");

        return group;
    }
}
