using deblog.Server.Common.Entities;

namespace deblog.Server.Features.Analytics;

public class DailyTelemetry : BaseEntity
{
    public DateOnly Date { get; set; }
    public int ViewsCount { get; set; } = 0;
    public int LikesCount { get; set; } = 0;
    public int SharesCount { get; set; } = 0;
    public int CommentsCount { get; set; } = 0;
}
