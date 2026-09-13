using System.Security.Claims;
using deblog.Server.Common.Data;
using deblog.Server.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class GetPostByIdOrSlugEndpoint
{
    public static RouteGroupBuilder MapGetPostByIdOrSlug(this RouteGroupBuilder group)
    {
        group.MapGet("/{idOrSlug}", async (
            string idOrSlug,
            ClaimsPrincipal user,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var baseUrl = (config["APP_BASE_URL"] ?? "http://localhost:4200").TrimEnd('/');
            var isAdmin = user.IsAdmin(config);

            var query = db.Posts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Analytics)
                .Include(p => p.Comments)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            Post? post;
            if (Guid.TryParse(idOrSlug, out var id))
            {
                post = await query.FirstOrDefaultAsync(p => p.Id == id, ct);
            }
            else
            {
                post = await query.FirstOrDefaultAsync(p => p.Slug == idOrSlug.ToLower(), ct);
            }

            if (post == null)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            // Public visitors can only view Published posts
            if (post.Status != PostStatus.Published && !isAdmin)
            {
                return Results.NotFound(new { message = "Post not found" });
            }

            return Results.Ok(PostHelpers.ToDetailDto(post, baseUrl));
        })
        .WithName("GetPostByIdOrSlug")
        .WithSummary("Get post details by ID or Slug (public visitors only see Published)");

        return group;
    }
}
