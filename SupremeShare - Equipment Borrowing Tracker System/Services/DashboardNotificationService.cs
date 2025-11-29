using Microsoft.AspNetCore.SignalR;
using SupremeShare___Equipment_Borrowing_Tracker_System.Hubs;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    /// <summary>
    /// Service for sending real-time dashboard notifications via SignalR
    /// </summary>
    public interface IDashboardNotificationService
    {
        Task NotifyStatsUpdated();
        Task NotifyNewTransaction(string transactionCode, string studentName, string action);
        Task NotifyOverdueAlert(int count);
        Task NotifyEquipmentStatusChange(string equipmentName, string serialNo, string newStatus);
    }

    public class DashboardNotificationService : IDashboardNotificationService
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardNotificationService> _logger;

        public DashboardNotificationService(
            IHubContext<DashboardHub> hubContext,
            ILogger<DashboardNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyStatsUpdated()
        {
            await _hubContext.Clients.All.SendAsync("StatsUpdated");
            _logger.LogDebug("Dashboard stats update notification sent");
        }

        public async Task NotifyNewTransaction(string transactionCode, string studentName, string action)
        {
            var notification = new
            {
                TransactionCode = transactionCode,
                StudentName = studentName,
                Action = action,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin", "support_admin").SendAsync("NewTransaction", notification);
            _logger.LogInformation("New transaction notification: {Code} - {Action}", transactionCode, action);
        }

        public async Task NotifyOverdueAlert(int count)
        {
            await _hubContext.Clients.Groups("admin", "support_admin").SendAsync("OverdueAlert", count);
            _logger.LogWarning("Overdue alert sent: {Count} items overdue", count);
        }

        public async Task NotifyEquipmentStatusChange(string equipmentName, string serialNo, string newStatus)
        {
            var notification = new
            {
                EquipmentName = equipmentName,
                SerialNo = serialNo,
                NewStatus = newStatus,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin").SendAsync("EquipmentStatusChanged", notification);
        }
    }
}
