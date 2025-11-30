using Microsoft.AspNetCore.SignalR;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time dashboard updates
    /// </summary>
    public class DashboardHub : Hub
    {
        private readonly ILogger<DashboardHub> _logger;

        public DashboardHub(ILogger<DashboardHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userRole = Context.GetHttpContext()?.Session.GetString("UserRole");
            var userId = Context.GetHttpContext()?.Session.GetInt32("UserId");

            if (!string.IsNullOrEmpty(userRole))
            {
                // Add to role-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
                _logger.LogInformation("User {UserId} ({Role}) connected to dashboard hub", userId, userRole);
            }

            // Add all authenticated users to 'all' group
            await Groups.AddToGroupAsync(Context.ConnectionId, "all");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userRole = Context.GetHttpContext()?.Session.GetString("UserRole");
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRole);
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Request dashboard stats refresh
        /// </summary>
        public async Task RequestStatsRefresh()
        {
            await Clients.Caller.SendAsync("RefreshStats");
        }

        /// <summary>
        /// Join a specific report room for targeted updates
        /// </summary>
        public async Task JoinReportRoom(string reportType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"report_{reportType}");
            _logger.LogDebug("Client joined room: report_{ReportType}", reportType);
        }

        /// <summary>
        /// Leave a report room
        /// </summary>
        public async Task LeaveReportRoom(string reportType)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"report_{reportType}");
            _logger.LogDebug("Client left room: report_{ReportType}", reportType);
        }
    }
}
