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
        Task NotifyAuditLogEntry(string action, string username);
        Task NotifyStudentUpdated(string studentName, string action);
        Task NotifyUserUpdated(string username, string action);
        Task NotifyEquipmentUpdated(string equipmentName, string action);
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
            await NotifyStatsUpdated();
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

            await _hubContext.Clients.Groups("admin", "support_admin").SendAsync("EquipmentStatusChanged", notification);
            await NotifyStatsUpdated();
            _logger.LogInformation("Equipment status changed: {Name} ({Serial}) -> {Status}", equipmentName, serialNo, newStatus);
        }

        public async Task NotifyAuditLogEntry(string action, string username)
        {
            var notification = new
            {
                Action = action,
                Username = username,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin").SendAsync("AuditLogEntry", notification);
            _logger.LogDebug("Audit log notification: {Action} by {User}", action, username);
        }

        public async Task NotifyStudentUpdated(string studentName, string action)
        {
            var notification = new
            {
                StudentName = studentName,
                Action = action,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin", "support_admin").SendAsync("StudentUpdated", notification);
            await NotifyStatsUpdated();
            _logger.LogInformation("Student updated: {Name} - {Action}", studentName, action);
        }

        public async Task NotifyUserUpdated(string username, string action)
        {
            var notification = new
            {
                Username = username,
                Action = action,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin").SendAsync("UserUpdated", notification);
            _logger.LogInformation("User updated: {Username} - {Action}", username, action);
        }

        public async Task NotifyEquipmentUpdated(string equipmentName, string action)
        {
            var notification = new
            {
                EquipmentName = equipmentName,
                Action = action,
                Timestamp = DateTime.Now.ToString("HH:mm:ss")
            };

            await _hubContext.Clients.Groups("admin", "support_admin").SendAsync("EquipmentStatusChanged", notification);
            await NotifyStatsUpdated();
            _logger.LogInformation("Equipment updated: {Name} - {Action}", equipmentName, action);
        }
    }
}
