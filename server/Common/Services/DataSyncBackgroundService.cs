namespace deblog.Server.Common.Services;

public class DataSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataSyncBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public DataSyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DataSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BG][DATA-SYNC] Data synchronization background service started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reconciler = scope.ServiceProvider.GetRequiredService<IDataReconciliationService>();
                var result = await reconciler.ReconcileAsync(stoppingToken);

                if (result.OrphanedCommentsHandled > 0 || result.OrphanedAnalyticsHandled > 0)
                {
                    _logger.LogInformation(
                        "[BG][DATA-SYNC] Reconciled deleted database records: {Comments} orphaned comments, {Analytics} orphaned analytics removed.",
                        result.OrphanedCommentsHandled,
                        result.OrphanedAnalyticsHandled);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "[ERR][SYSTEM][DATA-SYNC] Background synchronization task encountered an error: {Message}", ex.Message);
            }
        }

        _logger.LogInformation("[BG][DATA-SYNC] Data synchronization background service stopped.");
    }
}
