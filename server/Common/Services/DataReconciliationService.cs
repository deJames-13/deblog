using deblog.Server.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace deblog.Server.Common.Services;

public record ReconciliationResult(int OrphanedCommentsHandled, int OrphanedAnalyticsHandled, DateTime Timestamp);

public interface IDataReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(CancellationToken ct = default);
}

public class DataReconciliationService : IDataReconciliationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataReconciliationService> _logger;

    public DataReconciliationService(AppDbContext db, ILogger<DataReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReconciliationResult> ReconcileAsync(CancellationToken ct = default)
    {
        var orphanedCommentsCount = 0;
        var orphanedAnalyticsCount = 0;

        try
        {
            // 1. Detect comments pointing to non-existent posts (deleted externally in Supabase)
            var validPostIds = await _db.Posts
                .AsNoTracking()
                .Select(p => p.Id)
                .ToListAsync(ct);

            var orphanedComments = await _db.Comments
                .Where(c => !validPostIds.Contains(c.PostId))
                .ToListAsync(ct);

            if (orphanedComments.Count > 0)
            {
                _logger.LogInformation("[DATA-SYNC] Detected {Count} orphaned comments from deleted posts. Cleaning up...", orphanedComments.Count);
                _db.Comments.RemoveRange(orphanedComments);
                orphanedCommentsCount = orphanedComments.Count;
            }

            // 2. Detect analytics pointing to non-existent posts
            var orphanedAnalytics = await _db.PostAnalytics
                .Where(a => !validPostIds.Contains(a.PostId))
                .ToListAsync(ct);

            if (orphanedAnalytics.Count > 0)
            {
                _logger.LogInformation("[DATA-SYNC] Detected {Count} orphaned analytics records. Cleaning up...", orphanedAnalytics.Count);
                _db.PostAnalytics.RemoveRange(orphanedAnalytics);
                orphanedAnalyticsCount = orphanedAnalytics.Count;
            }

            if (orphanedCommentsCount > 0 || orphanedAnalyticsCount > 0)
            {
                await _db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERR][SYSTEM][DATA-SYNC] Error during database reconciliation: {Message}", ex.Message);
        }

        return new ReconciliationResult(orphanedCommentsCount, orphanedAnalyticsCount, DateTime.UtcNow);
    }
}
