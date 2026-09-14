using deblog.Server.Common.Data;
using deblog.Server.Common.Security.RateLimiting;
using deblog.Server.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Posts;

public static class TrackPostAnalyticsEndpoint
{
    public static RouteGroupBuilder MapTrackPostAnalytics(this RouteGroupBuilder group)
    {
        // POST /api/posts/{idOrSlug}/analytics/view
        group.MapPost("/{idOrSlug}/analytics/view", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackView(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Views++;
                await IncrementDailyTelemetryAsync(db, views: 1, ct: ct);
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { views = post.Analytics?.Views ?? 0 });
        })
        .WithName("TrackPostView")
        .WithSummary("Increment post view counter with visitor cooldown deduplication");

        // POST /api/posts/{idOrSlug}/analytics/like
        group.MapPost("/{idOrSlug}/analytics/like", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackLike(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Likes++;
                await IncrementDailyTelemetryAsync(db, likes: 1, ct: ct);
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { likes = post.Analytics?.Likes ?? 0 });
        })
        .WithName("TrackPostLike")
        .WithSummary("Increment post like counter with visitor cooldown deduplication")
        .RequireRateLimiting(RateLimitingPolicies.ReactionSpam);

        // POST /api/posts/{idOrSlug}/analytics/share
        group.MapPost("/{idOrSlug}/analytics/share", async (
            string idOrSlug,
            HttpContext httpContext,
            IAnalyticsTracker tracker,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var post = await ResolvePostWithAnalyticsAsync(idOrSlug, db, ct);
            if (post == null) return Results.NotFound(new { message = "Post not found" });

            var clientId = tracker.GetClientIdentifier(httpContext);
            if (tracker.CanTrackShare(clientId, post.Id))
            {
                post.Analytics ??= new PostAnalytics { PostId = post.Id };
                post.Analytics.Shares++;
                await IncrementDailyTelemetryAsync(db, shares: 1, ct: ct);
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok(new { shares = post.Analytics?.Shares ?? 0 });
        })
        .WithName("TrackPostShare")
        .WithSummary("Increment post share counter with visitor cooldown deduplication")
        .RequireRateLimiting(RateLimitingPolicies.ReactionSpam);

        return group;
    }

    private static async Task IncrementDailyTelemetryAsync(
        AppDbContext db,
        int views = 0,
        int likes = 0,
        int shares = 0,
        int comments = 0,
        CancellationToken ct = default)
    {
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

        telemetry.ViewsCount += views;
        telemetry.LikesCount += likes;
        telemetry.SharesCount += shares;
        telemetry.CommentsCount += comments;
    }

    private static async Task<Post?> ResolvePostWithAnalyticsAsync(string idOrSlug, AppDbContext db, CancellationToken ct)
    {
        if (Guid.TryParse(idOrSlug, out var id))
        {
            return await db.Posts
                .Include(p => p.Analytics)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        }

        return await db.Posts
            .Include(p => p.Analytics)
            .FirstOrDefaultAsync(p => p.Slug == idOrSlug.ToLower() && !p.IsDeleted, ct);
    }
}
