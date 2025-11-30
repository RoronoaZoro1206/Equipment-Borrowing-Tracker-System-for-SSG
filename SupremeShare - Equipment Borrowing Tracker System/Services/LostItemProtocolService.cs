using System.Data;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    public interface ILostItemProtocolService
    {
        Task<LostItemResult> MarkAsLostAsync(int borrowId, int userId, string? remarks = null);
        Task<List<LostItemRecord>> GetLostItemsAsync();
        Task<bool> UnblockStudentAsync(int studentId, int userId, string reason);
    }

    public class LostItemProtocolService : ILostItemProtocolService
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<LostItemProtocolService> _logger;

        public LostItemProtocolService(
            DatabaseHelper db,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<LostItemProtocolService> logger)
        {
            _db = db;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<LostItemResult> MarkAsLostAsync(int borrowId, int userId, string? remarks = null)
        {
            var result = new LostItemResult();

            try
            {
                // Get borrow record details first
                var detailsQuery = @"
                    SELECT b.borrow_id, b.eunit_id, t.student_id, t.transaction_code,
                           s.student_id_number, s.student_first_name, s.student_last_name,
                           e.equipment_name, eu.eunit_serial_no
                    FROM borrow_record b
                    JOIN transaction t ON b.transaction_id = t.transaction_id
                    JOIN student s ON t.student_id = s.student_id
                    JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                    JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE b.borrow_id = @borrow_id AND b.borrow_status IN ('borrowed', 'overdue')";

                var detailsParams = new NpgsqlParameter[] { new("@borrow_id", borrowId) };
                var dt = await _db.ExecuteQueryAsync(detailsQuery, detailsParams);

                if (dt.Rows.Count == 0)
                {
                    result.Success = false;
                    result.Message = "Borrow record not found or already processed";
                    return result;
                }

                var row = dt.Rows[0];
                result.StudentId = Convert.ToInt32(row["student_id"]);
                result.StudentIdNumber = row["student_id_number"]?.ToString();
                result.StudentName = $"{row["student_first_name"]} {row["student_last_name"]}";
                result.EquipmentName = row["equipment_name"]?.ToString();
                result.SerialNo = row["eunit_serial_no"]?.ToString();
                result.TransactionCode = row["transaction_code"]?.ToString();

                // Mark as lost - trigger will handle blocking student and updating unit
                var updateQuery = @"
                    UPDATE borrow_record 
                    SET borrow_status = 'lost', borrow_last_updated = NOW()
                    WHERE borrow_id = @borrow_id";

                await _db.ExecuteNonQueryAsync(updateQuery, detailsParams);

                // Log the action with remarks
                await _auditService.LogActionAsync(userId, "ITEM_MARKED_LOST", 
                    new { BorrowId = borrowId, Equipment = result.EquipmentName, SerialNo = result.SerialNo },
                    new { StudentBlocked = true, StudentId = result.StudentIdNumber, Remarks = remarks });

                // Send notifications
                await _notificationService.NotifyEquipmentStatusChange(
                    result.EquipmentName ?? "", 
                    result.SerialNo ?? "", 
                    "lost");
                await _notificationService.NotifyStatsUpdated();

                result.Success = true;
                result.Message = $"Item marked as lost. Student {result.StudentIdNumber} has been blocked.";
                result.StudentBlocked = true;

                _logger.LogWarning("Item marked as lost: {Equipment} ({Serial}). Student {StudentId} blocked.", 
                    result.EquipmentName, result.SerialNo, result.StudentIdNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking item as lost for borrow ID {BorrowId}", borrowId);
                result.Success = false;
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<List<LostItemRecord>> GetLostItemsAsync()
        {
            try
            {
                var query = @"
                    SELECT b.borrow_id, t.transaction_code, 
                           s.student_id, s.student_id_number, 
                           CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                           s.student_status,
                           e.equipment_name, eu.eunit_serial_no,
                           t.transaction_date_borrowed, b.borrow_last_updated as lost_date
                    FROM borrow_record b
                    JOIN transaction t ON b.transaction_id = t.transaction_id
                    JOIN student s ON t.student_id = s.student_id
                    JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                    JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE b.borrow_status = 'lost'
                    ORDER BY b.borrow_last_updated DESC";

                var dt = await _db.ExecuteQueryAsync(query);

                return dt.AsEnumerable().Select(row => new LostItemRecord
                {
                    BorrowId = Convert.ToInt32(row["borrow_id"]),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    StudentId = Convert.ToInt32(row["student_id"]),
                    StudentIdNumber = row["student_id_number"]?.ToString(),
                    StudentName = row["student_name"]?.ToString(),
                    StudentBlocked = !Convert.ToBoolean(row["student_status"]),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    SerialNo = row["eunit_serial_no"]?.ToString(),
                    BorrowDate = Convert.ToDateTime(row["transaction_date_borrowed"]),
                    LostDate = row["lost_date"] != DBNull.Value ? Convert.ToDateTime(row["lost_date"]) : null
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lost items");
                throw;
            }
        }

        public async Task<bool> UnblockStudentAsync(int studentId, int userId, string reason)
        {
            try
            {
                var query = @"
                    UPDATE student 
                    SET student_status = TRUE 
                    WHERE student_id = @student_id AND student_status = FALSE";

                var parameters = new NpgsqlParameter[] { new("@student_id", studentId) };
                var rowsAffected = await _db.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected > 0)
                {
                    await _auditService.LogActionAsync(userId, "STUDENT_UNBLOCKED", 
                        new { StudentId = studentId, WasBlocked = true },
                        new { StudentId = studentId, IsBlocked = false, Reason = reason });

                    _logger.LogInformation("Student {StudentId} unblocked by user {UserId}. Reason: {Reason}", 
                        studentId, userId, reason);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking student {StudentId}", studentId);
                throw;
            }
        }
    }

    public class LostItemResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string? StudentIdNumber { get; set; }
        public string? StudentName { get; set; }
        public string? EquipmentName { get; set; }
        public string? SerialNo { get; set; }
        public string? TransactionCode { get; set; }
        public bool StudentBlocked { get; set; }
    }

    public class LostItemRecord
    {
        public int BorrowId { get; set; }
        public string? TransactionCode { get; set; }
        public int StudentId { get; set; }
        public string? StudentIdNumber { get; set; }
        public string? StudentName { get; set; }
        public bool StudentBlocked { get; set; }
        public string? EquipmentName { get; set; }
        public string? SerialNo { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? LostDate { get; set; }
    }
}
