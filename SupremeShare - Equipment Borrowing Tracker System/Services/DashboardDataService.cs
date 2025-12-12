using System.Data;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models.DTOs;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services;

/// <summary>
/// Implementation of dashboard data service.
/// Centralizes all dashboard SQL queries for better maintainability, testability, and reusability.
/// </summary>
public class DashboardDataService : IDashboardDataService
{
    private readonly DatabaseHelper _db;
    private readonly ILogger<DashboardDataService> _logger;

    public DashboardDataService(DatabaseHelper db, ILogger<DashboardDataService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ===========================================
    // Support Admin Dashboard Methods
    // ===========================================

    public async Task<DashboardStats> GetSupportAdminStatsAsync()
    {
        try
        {
            await UpdateOverdueStatusesAsync();

            const string query = @"
                SELECT 
                    (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'borrowed') as borrowed,
                    (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned' 
                     AND DATE(borrow_date_returned) = CURRENT_DATE) as returned_today,
                    (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'overdue') as overdue,
                    (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'lost') as lost";

            var dt = await _db.ExecuteQueryAsync(query);

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return new DashboardStats(
                    Borrowed: Convert.ToInt32(row["borrowed"]),
                    ReturnedToday: Convert.ToInt32(row["returned_today"]),
                    Overdue: Convert.ToInt32(row["overdue"]),
                    Lost: Convert.ToInt32(row["lost"])
                );
            }

            return new DashboardStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting support admin dashboard stats");
            return new DashboardStats();
        }
    }

    public async Task<IEnumerable<BorrowRecordSummary>> GetBorrowRecordsByStatusAsync(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        try
        {
            // Handle "returned" status specially - only today's returns
            var statusCondition = status.Equals("returned", StringComparison.OrdinalIgnoreCase)
                ? "br.borrow_status = 'returned' AND DATE(br.borrow_date_returned) = CURRENT_DATE"
                : "LOWER(br.borrow_status) = LOWER(@status)";

            var query = $@"
                SELECT 
                    t.transaction_id,
                    t.transaction_code,
                    t.transaction_date_borrowed as date_borrowed,
                    s.student_id_number,
                    CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                    s.student_college,
                    s.student_course,
                    e.equipment_name,
                    e.equipment_category,
                    eu.eunit_serial_no as serial_no,
                    br.borrow_id,
                    br.borrow_status,
                    br.borrow_due_date,
                    br.borrow_date_returned
                FROM borrow_record br
                INNER JOIN transaction t ON br.transaction_id = t.transaction_id
                INNER JOIN student s ON t.student_id = s.student_id
                INNER JOIN equipment_unit eu ON br.eunit_id = eu.eunit_id
                INNER JOIN equipment e ON eu.equipment_id = e.equipment_id
                WHERE {statusCondition}
                ORDER BY t.transaction_date_borrowed DESC
                LIMIT 100";

            var parameters = status.Equals("returned", StringComparison.OrdinalIgnoreCase)
                ? Array.Empty<NpgsqlParameter>()
                : new[] { new NpgsqlParameter("@status", status) };

            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new BorrowRecordSummary(
                TransactionId: Convert.ToInt32(row["transaction_id"]),
                TransactionCode: row["transaction_code"]?.ToString(),
                DateBorrowed: row["date_borrowed"] != DBNull.Value ? Convert.ToDateTime(row["date_borrowed"]) : null,
                StudentIdNumber: row["student_id_number"]?.ToString(),
                StudentName: row["student_name"]?.ToString(),
                StudentCollege: row["student_college"]?.ToString(),
                StudentCourse: row["student_course"]?.ToString(),
                EquipmentName: row["equipment_name"]?.ToString(),
                EquipmentCategory: row["equipment_category"]?.ToString(),
                SerialNo: row["serial_no"]?.ToString(),
                BorrowId: Convert.ToInt32(row["borrow_id"]),
                Status: row["borrow_status"]?.ToString(),
                DueDate: row["borrow_due_date"] != DBNull.Value ? Convert.ToDateTime(row["borrow_due_date"]) : null,
                DateReturned: row["borrow_date_returned"] != DBNull.Value ? Convert.ToDateTime(row["borrow_date_returned"]) : null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting borrow records by status: {Status}", status);
            return Enumerable.Empty<BorrowRecordSummary>();
        }
    }

    public async Task<TransactionDetails?> GetTransactionDetailsAsync(string transactionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionCode);

        try
        {
            // Get transaction and student info
            const string txnQuery = @"
                SELECT 
                    t.transaction_id,
                    t.transaction_code,
                    t.transaction_date_borrowed,
                    t.transaction_remarks,
                    t.transaction_last_updated,
                    s.student_id_number,
                    CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                    s.student_college,
                    s.student_course,
                    s.student_email,
                    s.student_contact_no,
                    s.student_status,
                    CONCAT(u.user_first_name, ' ', u.user_last_name) as recorded_by
                FROM transaction t
                INNER JOIN student s ON t.student_id = s.student_id
                INNER JOIN ""USER"" u ON t.user_id = u.user_id
                WHERE t.transaction_code = @code";

            var txnDt = await _db.ExecuteQueryAsync(txnQuery, new[] { new NpgsqlParameter("@code", transactionCode) });

            if (txnDt.Rows.Count == 0)
                return null;

            var txnRow = txnDt.Rows[0];
            var transactionId = Convert.ToInt32(txnRow["transaction_id"]);

            // Get borrow records (items)
            var items = await GetTransactionItemsAsync(transactionId);

            // Get timeline from audit log
            var timeline = await GetTransactionTimelineAsync(transactionCode, txnRow);

            return new TransactionDetails(
                TransactionCode: txnRow["transaction_code"]?.ToString(),
                DateBorrowed: txnRow["transaction_date_borrowed"] != DBNull.Value 
                    ? Convert.ToDateTime(txnRow["transaction_date_borrowed"]) : null,
                Remarks: txnRow["transaction_remarks"]?.ToString(),
                LastUpdated: txnRow["transaction_last_updated"] != DBNull.Value 
                    ? Convert.ToDateTime(txnRow["transaction_last_updated"]) : null,
                RecordedBy: txnRow["recorded_by"]?.ToString(),
                Student: new StudentSummary(
                    IdNumber: txnRow["student_id_number"]?.ToString(),
                    Name: txnRow["student_name"]?.ToString(),
                    College: txnRow["student_college"]?.ToString(),
                    Course: txnRow["student_course"]?.ToString(),
                    Email: txnRow["student_email"]?.ToString(),
                    ContactNo: txnRow["student_contact_no"]?.ToString(),
                    Status: txnRow["student_status"] != DBNull.Value && Convert.ToBoolean(txnRow["student_status"])
                ),
                Items: items,
                Timeline: timeline
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction details: {Code}", transactionCode);
            return null;
        }
    }

    private async Task<IEnumerable<BorrowItemSummary>> GetTransactionItemsAsync(int transactionId)
    {
        const string query = @"
            SELECT 
                e.equipment_category,
                e.equipment_name,
                eu.eunit_serial_no,
                br.borrow_status,
                t.transaction_date_borrowed,
                br.borrow_due_date,
                br.borrow_date_returned,
                br.borrow_return_condition,
                br.borrow_last_updated
            FROM borrow_record br
            INNER JOIN transaction t ON br.transaction_id = t.transaction_id
            INNER JOIN equipment_unit eu ON br.eunit_id = eu.eunit_id
            INNER JOIN equipment e ON eu.equipment_id = e.equipment_id
            WHERE br.transaction_id = @txnId
            ORDER BY br.borrow_id";

        var dt = await _db.ExecuteQueryAsync(query, new[] { new NpgsqlParameter("@txnId", transactionId) });

        return dt.AsEnumerable().Select(row => new BorrowItemSummary(
            Category: row["equipment_category"]?.ToString(),
            Equipment: row["equipment_name"]?.ToString(),
            SerialNo: row["eunit_serial_no"]?.ToString(),
            Status: row["borrow_status"]?.ToString(),
            DateBorrowed: row["transaction_date_borrowed"] != DBNull.Value ? Convert.ToDateTime(row["transaction_date_borrowed"]) : null,
            DueDate: row["borrow_due_date"] != DBNull.Value ? Convert.ToDateTime(row["borrow_due_date"]) : null,
            DateReturned: row["borrow_date_returned"] != DBNull.Value ? Convert.ToDateTime(row["borrow_date_returned"]) : null,
            ReturnCondition: row["borrow_return_condition"] != DBNull.Value ? Convert.ToBoolean(row["borrow_return_condition"]) : null,
            LastUpdated: row["borrow_last_updated"] != DBNull.Value ? Convert.ToDateTime(row["borrow_last_updated"]) : null
        )).ToList();
    }

    private async Task<IEnumerable<TimelineEntry>> GetTransactionTimelineAsync(string transactionCode, DataRow txnRow)
    {
        const string query = @"
            SELECT 
                al.audit_logged_at as date, 
                al.audit_action as action, 
                COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'System') as performed_by
            FROM audit_log al
            LEFT JOIN ""USER"" u ON al.user_id = u.user_id
            WHERE al.audit_new_data LIKE @txnCode OR al.audit_old_data LIKE @txnCode
            ORDER BY al.audit_logged_at DESC
            LIMIT 20";

        var dt = await _db.ExecuteQueryAsync(query, new[] { new NpgsqlParameter("@txnCode", $"%{transactionCode}%") });

        var timeline = dt.AsEnumerable().Select(row => new TimelineEntry(
            Date: row["date"] != DBNull.Value ? Convert.ToDateTime(row["date"]) : null,
            Action: row["action"]?.ToString(),
            By: row["performed_by"]?.ToString()
        )).ToList();

        // Add default entry if no timeline exists
        if (timeline.Count == 0)
        {
            timeline.Add(new TimelineEntry(
                Date: txnRow["transaction_date_borrowed"] != DBNull.Value 
                    ? Convert.ToDateTime(txnRow["transaction_date_borrowed"]) : null,
                Action: "Borrowing Transaction Recorded",
                By: txnRow["recorded_by"]?.ToString()
            ));
        }

        return timeline;
    }

    public async Task<ChartData> GetChartDataAsync()
    {
        try
        {
            // Execute all queries in parallel for better performance
            var collegeTask = GetCollegeDataAsync();
            var categoryTask = GetCategoryDataAsync();
            var conditionTask = GetConditionDataAsync();
            var monthlyBorrowTask = GetMonthlyBorrowsAsync();
            var monthlyReturnTask = GetMonthlyReturnsAsync();

            await Task.WhenAll(collegeTask, categoryTask, conditionTask, monthlyBorrowTask, monthlyReturnTask);

            return new ChartData(
                CollegeData: await collegeTask,
                CategoryData: await categoryTask,
                ConditionData: await conditionTask,
                MonthlyBorrows: await monthlyBorrowTask,
                MonthlyReturns: await monthlyReturnTask
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chart data");
            return new ChartData(
                Enumerable.Empty<LabelValuePair>(),
                Enumerable.Empty<LabelValuePair>(),
                new ConditionData(0, 0),
                Enumerable.Empty<MonthlyData>(),
                Enumerable.Empty<MonthlyData>()
            );
        }
    }

    private async Task<IEnumerable<LabelValuePair>> GetCollegeDataAsync()
    {
        const string query = @"
            SELECT s.student_college, COUNT(DISTINCT t.student_id) as borrower_count
            FROM transaction t
            JOIN student s ON t.student_id = s.student_id
            WHERE s.student_college IS NOT NULL AND s.student_college != ''
            GROUP BY s.student_college
            ORDER BY borrower_count DESC
            LIMIT 10";

        var dt = await _db.ExecuteQueryAsync(query);
        return dt.AsEnumerable().Select(r => new LabelValuePair(
            r["student_college"]?.ToString() ?? "Unknown",
            Convert.ToInt32(r["borrower_count"])
        )).ToList();
    }

    private async Task<IEnumerable<LabelValuePair>> GetCategoryDataAsync()
    {
        // Fixed: Count actual equipment UNITS per category, not just equipment types
        const string query = @"
            SELECT e.equipment_category, COUNT(eu.eunit_id) as unit_count
            FROM equipment e
            LEFT JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
            WHERE e.equipment_category IS NOT NULL AND e.equipment_category != ''
            GROUP BY e.equipment_category
            ORDER BY unit_count DESC
            LIMIT 10";

        var dt = await _db.ExecuteQueryAsync(query);
        return dt.AsEnumerable().Select(r => new LabelValuePair(
            r["equipment_category"]?.ToString() ?? "Unknown",
            Convert.ToInt32(r["unit_count"])
        )).ToList();
    }

    private async Task<ConditionData> GetConditionDataAsync()
    {
        const string query = @"
            SELECT 
                COUNT(CASE WHEN eunit_condition = true THEN 1 END) as good_count,
                COUNT(CASE WHEN eunit_condition = false THEN 1 END) as damaged_count
            FROM equipment_unit";

        var dt = await _db.ExecuteQueryAsync(query);
        if (dt.Rows.Count > 0)
        {
            return new ConditionData(
                Good: Convert.ToInt32(dt.Rows[0]["good_count"]),
                Damaged: Convert.ToInt32(dt.Rows[0]["damaged_count"])
            );
        }
        return new ConditionData(0, 0);
    }

    private async Task<IEnumerable<MonthlyData>> GetMonthlyBorrowsAsync()
    {
        const string query = @"
            SELECT 
                TO_CHAR(transaction_date_borrowed, 'Mon') as month,
                EXTRACT(MONTH FROM transaction_date_borrowed) as month_num,
                COUNT(*) as borrow_count
            FROM transaction
            WHERE transaction_date_borrowed >= NOW() - INTERVAL '12 months'
            GROUP BY TO_CHAR(transaction_date_borrowed, 'Mon'), EXTRACT(MONTH FROM transaction_date_borrowed)
            ORDER BY month_num";

        var dt = await _db.ExecuteQueryAsync(query);
        return dt.AsEnumerable().Select(r => new MonthlyData(
            r["month"]?.ToString(),
            Convert.ToInt32(r["borrow_count"])
        )).ToList();
    }

    private async Task<IEnumerable<MonthlyData>> GetMonthlyReturnsAsync()
    {
        const string query = @"
            SELECT 
                TO_CHAR(borrow_date_returned, 'Mon') as month,
                EXTRACT(MONTH FROM borrow_date_returned) as month_num,
                COUNT(*) as return_count
            FROM borrow_record
            WHERE borrow_date_returned IS NOT NULL AND borrow_date_returned >= NOW() - INTERVAL '12 months'
            GROUP BY TO_CHAR(borrow_date_returned, 'Mon'), EXTRACT(MONTH FROM borrow_date_returned)
            ORDER BY month_num";

        var dt = await _db.ExecuteQueryAsync(query);
        return dt.AsEnumerable().Select(r => new MonthlyData(
            r["month"]?.ToString(),
            Convert.ToInt32(r["return_count"])
        )).ToList();
    }

    public async Task<IEnumerable<RecentActivity>> GetRecentActivityAsync()
    {
        try
        {
            const string query = @"
                SELECT 
                    t.transaction_code,
                    t.transaction_date_borrowed,
                    CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                    'borrow' as action_type,
                    e.equipment_name
                FROM transaction t
                JOIN student s ON t.student_id = s.student_id
                JOIN borrow_record br ON t.transaction_id = br.transaction_id
                JOIN equipment_unit eu ON br.eunit_id = eu.eunit_id
                JOIN equipment e ON eu.equipment_id = e.equipment_id
                WHERE t.transaction_date_borrowed >= NOW() - INTERVAL '7 days'
                
                UNION ALL
                
                SELECT 
                    t.transaction_code,
                    br.borrow_date_returned as activity_date,
                    CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                    'return' as action_type,
                    e.equipment_name
                FROM borrow_record br
                JOIN transaction t ON br.transaction_id = t.transaction_id
                JOIN student s ON t.student_id = s.student_id
                JOIN equipment_unit eu ON br.eunit_id = eu.eunit_id
                JOIN equipment e ON eu.equipment_id = e.equipment_id
                WHERE br.borrow_date_returned IS NOT NULL 
                  AND br.borrow_date_returned >= NOW() - INTERVAL '7 days'
                
                ORDER BY transaction_date_borrowed DESC
                LIMIT 20";

            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new RecentActivity(
                TransactionCode: row["transaction_code"]?.ToString(),
                Timestamp: row["transaction_date_borrowed"] != DBNull.Value 
                    ? Convert.ToDateTime(row["transaction_date_borrowed"]) : null,
                StudentName: row["student_name"]?.ToString(),
                ActionType: row["action_type"]?.ToString(),
                EquipmentName: row["equipment_name"]?.ToString()
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent activity");
            return Enumerable.Empty<RecentActivity>();
        }
    }

    // ===========================================
    // Admin Dashboard Methods
    // ===========================================

    public async Task<AdminDashboardStats> GetAdminStatsAsync()
    {
        try
        {
            const string query = @"
                SELECT 
                    (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'borrowed') as borrowed_count,
                    (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'available') as available_count,
                    (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'for review') as for_review_count,
                    (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'maintenance') as maintenance_count,
                    (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'lost') as lost_count";

            var dt = await _db.ExecuteQueryAsync(query);

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                return new AdminDashboardStats(
                    Borrowed: Convert.ToInt32(row["borrowed_count"]),
                    Available: Convert.ToInt32(row["available_count"]),
                    ForReview: Convert.ToInt32(row["for_review_count"]),
                    Maintenance: Convert.ToInt32(row["maintenance_count"]),
                    Lost: Convert.ToInt32(row["lost_count"])
                );
            }

            return new AdminDashboardStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin dashboard stats");
            return new AdminDashboardStats();
        }
    }

    public async Task<PaginatedResult<AuditLogEntry>> GetAuditLogsAsync(int page, int pageSize, string? actionFilter = null)
    {
        try
        {
            var offset = (page - 1) * pageSize;
            var whereClause = string.IsNullOrEmpty(actionFilter) ? "" : "WHERE a.audit_action ILIKE @action";

            var query = $@"
                SELECT a.audit_id, a.audit_action, a.audit_old_data, a.audit_new_data, a.audit_logged_at,
                       u.user_username, u.user_first_name, u.user_last_name
                FROM audit_log a
                LEFT JOIN ""USER"" u ON a.user_id = u.user_id
                {whereClause}
                ORDER BY a.audit_logged_at DESC
                LIMIT @limit OFFSET @offset";

            var parameters = new List<NpgsqlParameter>
            {
                new("@limit", pageSize),
                new("@offset", offset)
            };

            if (!string.IsNullOrEmpty(actionFilter))
                parameters.Add(new("@action", $"%{actionFilter}%"));

            var dt = await _db.ExecuteQueryAsync(query, parameters.ToArray());

            var data = dt.AsEnumerable().Select(row => new AuditLogEntry(
                AuditId: Convert.ToInt32(row["audit_id"]),
                Action: row["audit_action"]?.ToString(),
                OldData: row["audit_old_data"]?.ToString(),
                NewData: row["audit_new_data"]?.ToString(),
                LoggedAt: Convert.ToDateTime(row["audit_logged_at"]).ToString("yyyy-MM-dd HH:mm:ss"),
                Username: row["user_username"]?.ToString(),
                UserFullName: $"{row["user_first_name"]} {row["user_last_name"]}"
            )).ToList();

            // Get total count
            var countQuery = string.IsNullOrEmpty(actionFilter)
                ? "SELECT COUNT(*) FROM audit_log"
                : "SELECT COUNT(*) FROM audit_log WHERE audit_action ILIKE @action";
            var countParams = string.IsNullOrEmpty(actionFilter)
                ? Array.Empty<NpgsqlParameter>()
                : new[] { new NpgsqlParameter("@action", $"%{actionFilter}%") };

            var countDt = await _db.ExecuteQueryAsync(countQuery, countParams);
            var totalCount = Convert.ToInt32(countDt.Rows[0][0]);

            return new PaginatedResult<AuditLogEntry>(
                Data: data,
                Pagination: new PaginationInfo(
                    Page: page,
                    PageSize: pageSize,
                    TotalCount: totalCount,
                    TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return new PaginatedResult<AuditLogEntry>(
                Data: Enumerable.Empty<AuditLogEntry>(),
                Pagination: new PaginationInfo(page, pageSize, 0, 0)
            );
        }
    }

    // ===========================================
    // Shared Methods
    // ===========================================

    public async Task UpdateOverdueStatusesAsync()
    {
        try
        {
            await _db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update overdue statuses");
            // Don't throw - this shouldn't break the main operation
        }
    }
}
