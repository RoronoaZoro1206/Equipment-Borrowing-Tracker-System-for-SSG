namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models.DTOs;

/// <summary>
/// Dashboard statistics for Support Admin (borrow-focused)
/// </summary>
public record DashboardStats(
    int Borrowed = 0,
    int ReturnedToday = 0,
    int Overdue = 0,
    int Lost = 0
);

/// <summary>
/// Dashboard statistics for Admin (equipment unit status-focused)
/// </summary>
public record AdminDashboardStats(
    int Borrowed = 0,
    int Available = 0,
    int ForReview = 0,
    int Maintenance = 0,
    int Lost = 0
);

/// <summary>
/// Borrow record summary for dashboard display
/// </summary>
public record BorrowRecordSummary(
    int TransactionId,
    string? TransactionCode,
    DateTime? DateBorrowed,
    string? StudentIdNumber,
    string? StudentName,
    string? StudentCollege,
    string? StudentCourse,
    string? EquipmentName,
    string? EquipmentCategory,
    string? SerialNo,
    int BorrowId,
    string? Status,
    DateTime? DueDate,
    DateTime? DateReturned
);

/// <summary>
/// Transaction details for modal display
/// </summary>
public record TransactionDetails(
    string? TransactionCode,
    DateTime? DateBorrowed,
    string? Remarks,
    DateTime? LastUpdated,
    string? RecordedBy,
    StudentSummary Student,
    IEnumerable<BorrowItemSummary> Items,
    IEnumerable<TimelineEntry> Timeline
);

/// <summary>
/// Student summary within transaction details
/// </summary>
public record StudentSummary(
    string? IdNumber,
    string? Name,
    string? College,
    string? Course,
    string? Email,
    string? ContactNo,
    bool Status
);

/// <summary>
/// Borrowed item summary within transaction details
/// </summary>
public record BorrowItemSummary(
    string? Category,
    string? Equipment,
    string? SerialNo,
    string? Status,
    DateTime? DateBorrowed,
    DateTime? DueDate,
    DateTime? DateReturned,
    bool? ReturnCondition,
    DateTime? LastUpdated
);

/// <summary>
/// Timeline entry for transaction history
/// </summary>
public record TimelineEntry(
    DateTime? Date,
    string? Action,
    string? By
);

/// <summary>
/// Chart data for dashboard visualizations
/// </summary>
public record ChartData(
    IEnumerable<LabelValuePair> CollegeData,
    IEnumerable<LabelValuePair> CategoryData,
    ConditionData ConditionData,
    IEnumerable<MonthlyData> MonthlyBorrows,
    IEnumerable<MonthlyData> MonthlyReturns
);

/// <summary>
/// Generic label-value pair for charts
/// </summary>
public record LabelValuePair(string Label, int Value);

/// <summary>
/// Equipment condition summary (good vs damaged)
/// </summary>
public record ConditionData(int Good, int Damaged);

/// <summary>
/// Monthly transaction data for charts
/// </summary>
public record MonthlyData(string? Month, int Count);

/// <summary>
/// Recent activity entry for dashboard feed
/// </summary>
public record RecentActivity(
    string? TransactionCode,
    DateTime? Timestamp,
    string? StudentName,
    string? ActionType,
    string? EquipmentName
);

/// <summary>
/// Audit log entry for admin view
/// </summary>
public record AuditLogEntry(
    int AuditId,
    string? Action,
    string? OldData,
    string? NewData,
    string LoggedAt,
    string? Username,
    string? UserFullName
);

/// <summary>
/// Paginated result wrapper
/// </summary>
public record PaginatedResult<T>(
    IEnumerable<T> Data,
    PaginationInfo Pagination
);

/// <summary>
/// Pagination metadata
/// </summary>
public record PaginationInfo(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
