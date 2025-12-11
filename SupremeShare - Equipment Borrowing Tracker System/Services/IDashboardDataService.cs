using SupremeShare___Equipment_Borrowing_Tracker_System.Models.DTOs;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services;

/// <summary>
/// Service interface for dashboard data operations.
/// Centralizes all dashboard-related SQL queries for maintainability and testability.
/// </summary>
public interface IDashboardDataService
{
    // ===========================================
    // Support Admin Dashboard Methods
    // ===========================================
    
    /// <summary>
    /// Gets dashboard statistics for Support Admin (borrowed, returned today, overdue, lost)
    /// </summary>
    Task<DashboardStats> GetSupportAdminStatsAsync();
    
    /// <summary>
    /// Gets borrow records filtered by status
    /// </summary>
    /// <param name="status">Status to filter by (borrowed, returned, overdue, lost)</param>
    Task<IEnumerable<BorrowRecordSummary>> GetBorrowRecordsByStatusAsync(string status);
    
    /// <summary>
    /// Gets full transaction details by transaction code
    /// </summary>
    /// <param name="transactionCode">The unique transaction code</param>
    Task<TransactionDetails?> GetTransactionDetailsAsync(string transactionCode);
    
    /// <summary>
    /// Gets chart data for dashboard visualizations
    /// </summary>
    Task<ChartData> GetChartDataAsync();
    
    /// <summary>
    /// Gets recent transaction activity (last 7 days)
    /// </summary>
    Task<IEnumerable<RecentActivity>> GetRecentActivityAsync();

    // ===========================================
    // Admin Dashboard Methods
    // ===========================================
    
    /// <summary>
    /// Gets dashboard statistics for Admin (equipment status counts)
    /// </summary>
    Task<AdminDashboardStats> GetAdminStatsAsync();
    
    /// <summary>
    /// Gets paginated audit log entries
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="actionFilter">Optional filter by action type</param>
    Task<PaginatedResult<AuditLogEntry>> GetAuditLogsAsync(int page, int pageSize, string? actionFilter = null);

    // ===========================================
    // Shared Methods
    // ===========================================
    
    /// <summary>
    /// Updates overdue statuses for all borrowed items past due date
    /// </summary>
    Task UpdateOverdueStatusesAsync();
}
