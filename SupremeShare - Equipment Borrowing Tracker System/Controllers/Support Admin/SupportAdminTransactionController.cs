using System.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [Route("SupportAdmin/Transactions")]
    [SupportAdmin]
    public class SupportAdminTransactionController : Controller
    {
        private readonly ITransactionRepository _transactionRepo;
        private readonly IBorrowRecordRepository _borrowRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEquipmentRepository _equipmentRepo;
        private readonly IEquipmentUnitRepository _unitRepo;
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<SupportAdminTransactionController> _logger;

        public SupportAdminTransactionController(
            ITransactionRepository transactionRepo,
            IBorrowRecordRepository borrowRepo,
            IStudentRepository studentRepo,
            IEquipmentRepository equipmentRepo,
            IEquipmentUnitRepository unitRepo,
            DatabaseHelper db,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<SupportAdminTransactionController> logger)
        {
            _transactionRepo = transactionRepo;
            _borrowRepo = borrowRepo;
            _studentRepo = studentRepo;
            _equipmentRepo = equipmentRepo;
            _unitRepo = unitRepo;
            _db = db;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: SupportAdmin/Transactions
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var transactions = await _transactionRepo.GetAllAsync();
            return View("~/Views/SupportAdmin/Transactions/Index.cshtml", transactions);
        }

        // GET: SupportAdmin/Transactions/Borrow
        [HttpGet("Borrow")]
        public IActionResult Borrow()
        {
            return View("~/Views/SupportAdmin/Transactions/Borrow.cshtml");
        }


        // GET: SupportAdmin/Transactions/Return
        [HttpGet("Return")]
        public IActionResult Return()
        {
            return View("~/Views/SupportAdmin/Transactions/Return.cshtml");
        }

        #region API Endpoints

        // GET: SupportAdmin/Transactions/GetAll
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var query = @"
                    SELECT t.transaction_id, t.transaction_code, t.transaction_date_borrowed,
                           s.student_id_number, s.student_first_name, s.student_last_name,
                           u.user_username,
                           COUNT(b.borrow_id) as item_count,
                           STRING_AGG(DISTINCT b.borrow_status, ', ') as statuses
                    FROM transaction t
                    JOIN student s ON t.student_id = s.student_id
                    JOIN ""USER"" u ON t.user_id = u.user_id
                    LEFT JOIN borrow_record b ON t.transaction_id = b.transaction_id
                    GROUP BY t.transaction_id, s.student_id_number, s.student_first_name, 
                             s.student_last_name, u.user_username
                    ORDER BY t.transaction_id DESC
                    LIMIT 100";

                var dt = await _db.ExecuteQueryAsync(query);
                var result = dt.AsEnumerable().Select(row => new
                {
                    TransactionId = Convert.ToInt32(row["transaction_id"]),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    DateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]).ToString("yyyy-MM-dd HH:mm"),
                    StudentIdNumber = row["student_id_number"]?.ToString(),
                    StudentName = $"{row["student_first_name"]} {row["student_last_name"]}",
                    ProcessedBy = row["user_username"]?.ToString(),
                    ItemCount = Convert.ToInt32(row["item_count"]),
                    Statuses = row["statuses"]?.ToString()
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transactions");
                return Json(new { success = false, message = "Failed to fetch transactions" });
            }
        }

        // GET: SupportAdmin/Transactions/GetDetails/{id}
        [HttpGet("GetDetails/{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var transaction = await _transactionRepo.GetByIdAsync(id);
                if (transaction == null)
                    return Json(new { success = false, message = "Transaction not found" });

                var borrowRecords = await _borrowRepo.GetByTransactionIdAsync(id);
                var student = await _studentRepo.GetByIdAsync(transaction.StudentId);

                var items = new List<object>();
                foreach (var record in borrowRecords)
                {
                    var unit = await _unitRepo.GetByIdAsync(record.EunitId);
                    var equipment = unit != null ? await _equipmentRepo.GetByIdAsync(unit.EquipmentId) : null;

                    items.Add(new
                    {
                        record.BorrowId,
                        record.BorrowStatus,
                        record.BorrowDueDate,
                        record.BorrowDateReturned,
                        record.BorrowReturnCondition,
                        SerialNo = unit?.EunitSerialNo,
                        EquipmentName = equipment?.EquipmentName,
                        DaysOverdue = record.BorrowStatus == "overdue" || record.BorrowStatus == "borrowed"
                            ? Math.Max(0, (DateTime.Now - record.BorrowDueDate).Days) : 0
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        transaction.TransactionId,
                        transaction.TransactionCode,
                        transaction.TransactionDateBorrowed,
                        transaction.TransactionRemarks,
                        Student = new { student?.StudentIdNumber, student?.FullName, student?.StudentCollege },
                        Items = items
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction details");
                return Json(new { success = false, message = "Failed to fetch details" });
            }
        }

        // GET: SupportAdmin/Transactions/SearchByCode?code=xxx
        [HttpGet("SearchByCode")]
        public async Task<IActionResult> SearchByCode([FromQuery] string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return Json(new { success = false, message = "Code is required" });

                var transaction = await _transactionRepo.GetByCodeAsync(code);
                if (transaction == null)
                    return Json(new { success = false, message = "Transaction not found" });

                return await GetDetails(transaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching transaction");
                return Json(new { success = false, message = "Search failed" });
            }
        }

        // GET: SupportAdmin/Transactions/GetActiveBorrows
        [HttpGet("GetActiveBorrows")]
        public async Task<IActionResult> GetActiveBorrows()
        {
            try
            {
                var query = @"
                    SELECT b.borrow_id, b.borrow_status, b.borrow_due_date,
                           t.transaction_code, t.transaction_date_borrowed,
                           s.student_id_number, s.student_first_name, s.student_last_name,
                           eu.eunit_serial_no, e.equipment_name
                    FROM borrow_record b
                    JOIN transaction t ON b.transaction_id = t.transaction_id
                    JOIN student s ON t.student_id = s.student_id
                    JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                    JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE b.borrow_status IN ('borrowed', 'overdue')
                    ORDER BY b.borrow_due_date ASC";

                var dt = await _db.ExecuteQueryAsync(query);
                var result = dt.AsEnumerable().Select(row => new
                {
                    BorrowId = Convert.ToInt32(row["borrow_id"]),
                    Status = row["borrow_status"]?.ToString(),
                    DueDate = Convert.ToDateTime(row["borrow_due_date"]).ToString("yyyy-MM-dd"),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    StudentIdNumber = row["student_id_number"]?.ToString(),
                    StudentName = $"{row["student_first_name"]} {row["student_last_name"]}",
                    SerialNo = row["eunit_serial_no"]?.ToString(),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    DaysOverdue = Math.Max(0, (DateTime.Now - Convert.ToDateTime(row["borrow_due_date"])).Days)
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active borrows");
                return Json(new { success = false, message = "Failed to fetch active borrows" });
            }
        }


        // POST: SupportAdmin/Transactions/ProcessBorrow
        [HttpPost("ProcessBorrow")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBorrow([FromForm] SupportBorrowRequest request)
        {
            try
            {
                // Validate student
                var student = await _studentRepo.GetByIdAsync(request.StudentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });
                if (!student.StudentStatus)
                    return Json(new { success = false, message = "Student is blocked from borrowing" });

                // Validate unit
                var unit = await _unitRepo.GetByIdAsync(request.UnitId);
                if (unit == null)
                    return Json(new { success = false, message = "Equipment unit not found" });
                if (unit.EunitStatus != "available")
                    return Json(new { success = false, message = "Unit is not available" });

                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var dueDate = request.DueDate ?? DateTime.Now.AddDays(7);

                // Call stored procedure
                var query = "CALL sp_process_borrow_by_unit(@p_student_id, @p_user_id, @p_eunit_id, @p_due_date, NULL, NULL)";
                var parameters = new NpgsqlParameter[]
                {
                    new("@p_student_id", request.StudentId),
                    new("@p_user_id", userId),
                    new("@p_eunit_id", request.UnitId),
                    new("@p_due_date", dueDate)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);

                // Get the created transaction code
                var codeQuery = @"SELECT transaction_code FROM transaction 
                                  WHERE student_id = @sid ORDER BY transaction_id DESC LIMIT 1";
                var codeParams = new NpgsqlParameter[] { new("@sid", request.StudentId) };
                var dt = await _db.ExecuteQueryAsync(codeQuery, codeParams);
                var transactionCode = dt.Rows.Count > 0 ? dt.Rows[0]["transaction_code"]?.ToString() : "Unknown";

                await _auditService.LogActionAsync(userId, "BORROW_PROCESSED_BY_SUPPORT", null,
                    new { request.StudentId, request.UnitId, TransactionCode = transactionCode });

                _logger.LogInformation("Support Admin borrow processed: Student {StudentId}, Unit {UnitId}, Code {Code}",
                    request.StudentId, request.UnitId, transactionCode);

                // Send real-time notification
                await _notificationService.NotifyNewTransaction(transactionCode ?? "Unknown", student.FullName, "BORROW");
                await _notificationService.NotifyStatsUpdated();

                return Json(new { success = true, message = "Borrow processed successfully", transactionCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing borrow");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: SupportAdmin/Transactions/ProcessReturn
        [HttpPost("ProcessReturn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn([FromForm] SupportReturnRequest request)
        {
            try
            {
                var borrowRecord = await _borrowRepo.GetByIdAsync(request.BorrowId);
                if (borrowRecord == null)
                    return Json(new { success = false, message = "Borrow record not found" });
                if (borrowRecord.BorrowStatus == "returned")
                    return Json(new { success = false, message = "Item already returned" });

                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;

                // Call stored procedure
                var query = "CALL sp_process_return_boolean(@p_borrow_id, @p_return_condition, @p_user_id)";
                var parameters = new NpgsqlParameter[]
                {
                    new("@p_borrow_id", request.BorrowId),
                    new("@p_return_condition", request.Condition),
                    new("@p_user_id", userId)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);

                var conditionText = request.Condition ? "Good" : "Damaged";
                await _auditService.LogActionAsync(userId, "RETURN_PROCESSED_BY_SUPPORT", null,
                    new { request.BorrowId, Condition = conditionText });

                _logger.LogInformation("Support Admin return processed: BorrowId {BorrowId}, Condition {Condition}",
                    request.BorrowId, conditionText);

                // Send real-time notification
                await _notificationService.NotifyStatsUpdated();

                return Json(new { success = true, message = $"Return processed. Condition: {conditionText}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: SupportAdmin/Transactions/UpdateOverdue
        [HttpPost("UpdateOverdue")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOverdue()
        {
            try
            {
                await _db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()");
                _logger.LogInformation("Overdue records updated by support admin");
                return Json(new { success = true, message = "Overdue records updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overdue records");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: SupportAdmin/Transactions/Stats
        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'borrowed') as active_borrows,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'overdue') as overdue_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned' 
                         AND DATE(borrow_date_returned) = CURRENT_DATE) as returned_today,
                        (SELECT COUNT(*) FROM transaction WHERE DATE(transaction_date_borrowed) = CURRENT_DATE) as borrowed_today,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'lost') as lost_count";

                var dt = await _db.ExecuteQueryAsync(query);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            ActiveBorrows = Convert.ToInt32(row["active_borrows"]),
                            OverdueCount = Convert.ToInt32(row["overdue_count"]),
                            ReturnedToday = Convert.ToInt32(row["returned_today"]),
                            BorrowedToday = Convert.ToInt32(row["borrowed_today"]),
                            LostCount = Convert.ToInt32(row["lost_count"])
                        }
                    });
                }

                return Json(new { success = false, message = "No stats available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stats");
                return Json(new { success = false, message = "Failed to fetch stats" });
            }
        }

        #endregion
    }

    // Request models for Support Admin (no MarkLost capability)
    public class SupportBorrowRequest
    {
        public int StudentId { get; set; }
        public int UnitId { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class SupportReturnRequest
    {
        public int BorrowId { get; set; }
        public bool Condition { get; set; } = true;
    }
}
