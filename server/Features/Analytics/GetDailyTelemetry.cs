using deblog.Server.Common.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Features.Analytics;

public static class GetDailyTelemetryEndpoint
{
    public static RouteGroupBuilder MapGetDailyTelemetry(this RouteGroupBuilder group)
    {
        group.MapGet("/telemetry", async (
            [FromQuery] int? days,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var dayCount = days is > 0 and <= 30 ? days.Value : 7;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var startDate = today.AddDays(-(dayCount - 1));

            var records = await db.DailyTelemetries
                .AsNoTracking()
                .Where(t => t.Date >= startDate && t.Date <= today)
                .ToDictionaryAsync(t => t.Date, ct);

            var daysList = new List<DailyTelemetryDto>();
            var totalViews = 0;
            var totalLikes = 0;
            var totalComments = 0;
            var peakViews = 0;

            for (var d = startDate; d <= today; d = d.AddDays(1))
            {
                records.TryGetValue(d, out var item);
                var views = item?.ViewsCount ?? 0;
                var likes = item?.LikesCount ?? 0;
                var shares = item?.SharesCount ?? 0;
                var comments = item?.CommentsCount ?? 0;

                totalViews += views;
                totalLikes += likes;
                totalComments += comments;
                if (views > peakViews) peakViews = views;

                daysList.Add(new DailyTelemetryDto(
                    Day: d.ToString("ddd"),
                    Date: d.ToString("yyyy-MM-dd"),
                    Views: views,
                    Likes: likes,
                    Shares: shares,
                    Comments: comments
                ));
            }

            var summary = new TelemetrySummaryDto(
                Days: daysList,
                TotalViews7Days: totalViews,
                PeakViews: Math.Max(peakViews, 10), // sensible visual peak baseline
                TotalComments7Days: totalComments,
                TotalLikes7Days: totalLikes
            );

            return Results.Ok(summary);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetDailyTelemetry")
        .WithSummary("Retrieve chronological daily access telemetry with gap filling (Admin Only)");

        return group;
    }
}
