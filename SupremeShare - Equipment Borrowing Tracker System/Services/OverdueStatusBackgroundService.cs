using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    /// <summary>
    /// Background service to automatically update overdue status of borrowed items
    /// </summary>
    public class OverdueStatusBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueStatusBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public OverdueStatusBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OverdueStatusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Status Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateOverdueStatuses();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Overdue Status Background Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Overdue Status Background Service stopped");
        }

        private async Task UpdateOverdueStatuses()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DatabaseHelper>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IDashboardNotificationService>();

            try
            {
                var countQuery = @"
                    SELECT COUNT(*) as overdue_count
                    FROM borrow_record 
                    WHERE borrow_status = 'borrowed' AND borrow_due_date < NOW()";
                
                var countDt = await db.ExecuteQueryAsync(countQuery);
                var overdueCount = Convert.ToInt32(countDt.Rows[0]["overdue_count"]);

                if (overdueCount > 0)
                {
                    await db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()");
                    _logger.LogInformation("Updated {Count} items to overdue status", overdueCount);
                    
                    await notificationService.NotifyOverdueAlert(overdueCount);
                    await notificationService.NotifyStatsUpdated();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overdue statuses");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Status Background Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
