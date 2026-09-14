namespace deblog.Server.Features.Analytics;

public record DailyTelemetryDto(
    string Day,
    string Date,
    int Views,
    int Likes,
    int Shares,
    int Comments
);

public record TelemetrySummaryDto(
    IReadOnlyList<DailyTelemetryDto> Days,
    int TotalViews7Days,
    int PeakViews,
    int TotalComments7Days,
    int TotalLikes7Days
);
