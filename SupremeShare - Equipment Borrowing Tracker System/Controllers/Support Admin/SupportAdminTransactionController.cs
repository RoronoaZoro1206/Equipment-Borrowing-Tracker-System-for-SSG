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
                // Auto-update overdue statuses before fetching
                await UpdateOverdueStatusesInternal();

                var query = @"
                    SELECT t.transaction_id, t.transaction_code, t.transaction_date_borrowed,
                           t.transaction_remarks, t.transaction_last_updated,
                           s.student_id_number, s.student_first_name, s.student_last_name,
                           CONCAT(u.user_first_name, ' ', u.user_last_name) as recorded_by,
                           COUNT(b.borrow_id) as item_count,
                           STRING_AGG(DISTINCT b.borrow_status, ', ') as statuses
                    FROM transaction t
                    JOIN student s ON t.student_id = s.student_id
                    JOIN ""USER"" u ON t.user_id = u.user_id
                    LEFT JOIN borrow_record b ON t.transaction_id = b.transaction_id
                    GROUP BY t.transaction_id, s.student_id_number, s.student_first_name, 
                             s.student_last_name, u.user_first_name, u.user_last_name,
                             t.transaction_remarks, t.transaction_last_updated
                    ORDER BY t.transaction_id DESC
                    LIMIT 100";

                var dt = await _db.ExecuteQueryAsync(query);
                var result = dt.AsEnumerable().Select(row => new
                {
                    TransactionId = Convert.ToInt32(row["transaction_id"]),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    DateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]).ToString("yyyy-MM-dd HH:mm"),
                    LastUpdated = row["transaction_last_updated"] != DBNull.Value 
                        ? Convert.ToDateTime(row["transaction_last_updated"]).ToString("yyyy-MM-dd HH:mm") 
                        : null,
                    Remarks = row["transaction_remarks"]?.ToString(),
                    StudentIdNumber = row["student_id_number"]?.ToString(),
                    StudentName = $"{row["student_first_name"]} {row["student_last_name"]}",
                    ProcessedBy = row["recorded_by"]?.ToString(),
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

                // Get recorded by user
                var userQuery = @"SELECT user_username, CONCAT(user_first_name, ' ', user_last_name) as full_name 
                                  FROM ""USER"" WHERE user_id = @userId";
                var userDt = await _db.ExecuteQueryAsync(userQuery, new[] { new NpgsqlParameter("@userId", transaction.UserId) });
                var recordedBy = userDt.Rows.Count > 0 ? userDt.Rows[0]["full_name"]?.ToString() : "Unknown";

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
                        BorrowLastUpdated = record.BorrowLastUpdated,
                        SerialNo = unit?.EunitSerialNo,
                        EquipmentName = equipment?.EquipmentName,
                        EquipmentCategory = equipment?.EquipmentCategory,
                        DaysOverdue = record.BorrowStatus == "overdue" || record.BorrowStatus == "borrowed"
                            ? Math.Max(0, (DateTime.Now - record.BorrowDueDate).Days) : 0
                    });
                }

                // Get timeline from audit log
                var timelineQuery = @"
                    SELECT al.audit_logged_at as date, al.audit_action as action, 
                           CONCAT(u.user_first_name, ' ', u.user_last_name) as by
                    FROM audit_log al
                    LEFT JOIN ""USER"" u ON al.user_id = u.user_id
                    WHERE al.audit_new_data LIKE @txnCode OR al.audit_old_data LIKE @txnCode
                    ORDER BY al.audit_logged_at DESC
                    LIMIT 20";
                var timelineDt = await _db.ExecuteQueryAsync(timelineQuery, 
                    new[] { new NpgsqlParameter("@txnCode", $"%{transaction.TransactionCode}%") });
                
                var timeline = new List<object>();
                foreach (DataRow row in timelineDt.Rows)
                {
                    timeline.Add(new
                    {
                        date = row["date"] != DBNull.Value ? Convert.ToDateTime(row["date"]) : (DateTime?)null,
                        action = row["action"]?.ToString(),
                        by = row["by"]?.ToString() ?? "System"
                    });
                }

                // Add default timeline entry if empty
                if (timeline.Count == 0)
                {
                    timeline.Add(new
                    {
                        date = (DateTime?)transaction.TransactionDateBorrowed,
                        action = "Borrowing Transaction Recorded",
                        by = recordedBy
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
                        TransactionLastUpdated = transaction.TransactionLastUpdated,
                        RecordedBy = recordedBy,
                        Student = new 
                        { 
                            student?.StudentIdNumber, 
                            student?.FullName, 
                            student?.StudentCollege,
                            StudentCourse = student?.StudentCourse,
                            StudentEmail = student?.StudentEmail,
                            StudentContactNo = student?.StudentContactNo,
                            StudentStatus = student?.StudentStatus
                        },
                        Items = items,
                        Timeline = timeline
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
                // Auto-update overdue statuses before fetching
                await UpdateOverdueStatusesInternal();

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
                // Validate student eligibility using database function fn_check_student_eligibility
                var isEligible = await _studentRepo.CheckEligibilityAsync(request.StudentId);
                if (!isEligible)
                    return Json(new { success = false, message = "Student is not eligible to borrow (blocked or not found)" });
                
                var student = await _studentRepo.GetByIdAsync(request.StudentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

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

                // Call stored procedure (sp_process_return_boolean)
                // If condition is FALSE (Damaged), unit status becomes 'for review'
                var query = "CALL sp_process_return_boolean(@p_borrow_id, @p_return_condition, @p_user_id)";
                var parameters = new NpgsqlParameter[]
                {
                    new("@p_borrow_id", request.BorrowId),
                    new("@p_return_condition", request.Condition),
                    new("@p_user_id", userId)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);

                var conditionText = request.Condition ? "Good" : "Damaged";
                var sentForReview = !request.Condition;
                
                await _auditService.LogActionAsync(userId, "RETURN_PROCESSED_BY_SUPPORT", null,
                    new { request.BorrowId, Condition = conditionText, SentForReview = sentForReview });

                _logger.LogInformation("Support Admin return processed: BorrowId {BorrowId}, Condition {Condition}, SentForReview {SentForReview}",
                    request.BorrowId, conditionText, sentForReview);

                // Send real-time notification
                await _notificationService.NotifyStatsUpdated();

                // Return appropriate message based on condition
                var message = request.Condition 
                    ? "Item returned successfully and is now available." 
                    : "Item returned and sent for review due to reported damage.";

                return Json(new { 
                    success = true, 
                    message = message,
                    sentForReview = sentForReview,
                    condition = conditionText
                });
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
                await UpdateOverdueStatusesInternal();
                _logger.LogInformation("Overdue records updated by support admin");
                return Json(new { success = true, message = "Overdue records updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overdue records");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: SupportAdmin/Transactions/UpdateRemarks
        [HttpPost("UpdateRemarks")]
        public async Task<IActionResult> UpdateRemarks([FromForm] int transactionId, [FromForm] string? remarks)
        {
            try
            {
                var query = @"UPDATE transaction 
                              SET transaction_remarks = @remarks, 
                                  transaction_last_updated = NOW() 
                              WHERE transaction_id = @transactionId";
                var parameters = new NpgsqlParameter[]
                {
                    new("@remarks", (object?)remarks ?? DBNull.Value),
                    new("@transactionId", transactionId)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);

                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                await _auditService.LogActionAsync(userId, "TRANSACTION_REMARKS_UPDATED", null,
                    new { TransactionId = transactionId, Remarks = remarks });

                _logger.LogInformation("Transaction {TransactionId} remarks updated by support admin", transactionId);
                return Json(new { success = true, message = "Remarks updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction remarks");
                return Json(new { success = false, message = "Failed to update remarks" });
            }
        }

        // POST: SupportAdmin/Transactions/UpdateBorrowRecord
        [HttpPost("UpdateBorrowRecord")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBorrowRecord(
            [FromForm] int borrowId,
            [FromForm] string status,
            [FromForm] DateTime dueDate,
            [FromForm] DateTime? dateReturned,
            [FromForm] string? returnCondition)
        {
            try
            {
                var borrowRecord = await _borrowRepo.GetByIdAsync(borrowId);
                if (borrowRecord == null)
                    return Json(new { success = false, message = "Borrow record not found" });

                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var oldStatus = borrowRecord.BorrowStatus;

                // Parse return condition
                bool? condition = null;
                if (!string.IsNullOrEmpty(returnCondition))
                {
                    condition = returnCondition.ToLower() == "true";
                }

                // Build update query
                var query = @"UPDATE borrow_record 
                              SET borrow_status = @status,
                                  borrow_due_date = @dueDate,
                                  borrow_date_returned = @dateReturned,
                                  borrow_return_condition = @returnCondition,
                                  borrow_last_updated = NOW()
                              WHERE borrow_id = @borrowId";

                var parameters = new NpgsqlParameter[]
                {
                    new("@status", status),
                    new("@dueDate", dueDate),
                    new("@dateReturned", (object?)dateReturned ?? DBNull.Value),
                    new("@returnCondition", (object?)condition ?? DBNull.Value),
                    new("@borrowId", borrowId)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);

                // If status changed to 'returned', update equipment unit status
                if (status == "returned" && oldStatus != "returned")
                {
                    var unitQuery = @"UPDATE equipment_unit 
                                      SET eunit_status = CASE WHEN @condition = false THEN 'damaged' ELSE 'available' END
                                      WHERE eunit_id = (SELECT eunit_id FROM borrow_record WHERE borrow_id = @borrowId)";
                    await _db.ExecuteNonQueryAsync(unitQuery, new NpgsqlParameter[]
                    {
                        new("@condition", (object?)condition ?? DBNull.Value),
                        new("@borrowId", borrowId)
                    });
                }

                // Update parent transaction's last_updated
                var txnQuery = @"UPDATE transaction 
                                 SET transaction_last_updated = NOW() 
                                 WHERE transaction_id = (SELECT transaction_id FROM borrow_record WHERE borrow_id = @borrowId)";
                await _db.ExecuteNonQueryAsync(txnQuery, new[] { new NpgsqlParameter("@borrowId", borrowId) });

                await _auditService.LogActionAsync(userId, "BORROW_RECORD_UPDATED", null,
                    new { BorrowId = borrowId, OldStatus = oldStatus, NewStatus = status, DueDate = dueDate, DateReturned = dateReturned, ReturnCondition = condition });

                _logger.LogInformation("Borrow record {BorrowId} updated: Status={Status}, DueDate={DueDate}", borrowId, status, dueDate);

                // Send real-time notification
                await _notificationService.NotifyStatsUpdated();

                return Json(new { success = true, message = "Borrow record updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating borrow record");
                return Json(new { success = false, message = "Failed to update borrow record: " + ex.Message });
            }
        }

        // POST: SupportAdmin/Transactions/SendTransactionEmail
        [HttpPost("SendTransactionEmail")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTransactionEmail([FromForm] string transactionCode, [FromForm] string studentEmail, [FromForm] string studentName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studentEmail))
                    return Json(new { success = false, message = "Student email is required" });

                // Get transaction details
                var transaction = await _transactionRepo.GetByCodeAsync(transactionCode);
                if (transaction == null)
                    return Json(new { success = false, message = "Transaction not found" });

                var borrowRecords = await _borrowRepo.GetByTransactionIdAsync(transaction.TransactionId);
                
                // Build items list for email
                var itemsList = new System.Text.StringBuilder();
                foreach (var record in borrowRecords)
                {
                    var unit = await _unitRepo.GetByIdAsync(record.EunitId);
                    var equipment = unit != null ? await _equipmentRepo.GetByIdAsync(unit.EquipmentId) : null;
                    itemsList.AppendLine($"• {equipment?.EquipmentName ?? "Unknown"} (Serial: {unit?.EunitSerialNo ?? "N/A"}) - Due: {record.BorrowDueDate:MMM dd, yyyy}");
                }

                // Send email using SMTP
                var emailSent = await SendTransactionEmailAsync(
                    studentEmail, 
                    studentName, 
                    transactionCode, 
                    transaction.TransactionDateBorrowed, 
                    itemsList.ToString()
                );

                if (emailSent)
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                    await _auditService.LogActionAsync(userId, "TRANSACTION_EMAIL_SENT", null,
                        new { TransactionCode = transactionCode, StudentEmail = studentEmail });

                    _logger.LogInformation("Transaction email sent for {TransactionCode} to {Email}", transactionCode, studentEmail);
                    return Json(new { success = true, message = "Email sent successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to send email. Please check SMTP configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction email");
                return Json(new { success = false, message = "Failed to send email" });
            }
        }

        private async Task<bool> SendTransactionEmailAsync(string toEmail, string studentName, string transactionCode, DateTime dateBorrowed, string itemsList)
        {
            try
            {
                var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
                var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
                var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
                var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
                var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? smtpUsername;
                var fromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "SupremeShare System";

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured");
                    return false;
                }

                var subject = $"SupremeShare - Borrowing Transaction Confirmation [{transactionCode}]";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333333; background-color: #f4f4f4;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color: #f4f4f4;'>
        <tr>
            <td align='center' style='padding: 40px 20px;'>
                <table role='presentation' width='600' cellspacing='0' cellpadding='0' border='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background-color: #8B0000; padding: 30px 40px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 28px; font-weight: bold;'>SupremeShare</h1>
                            <p style='margin: 10px 0 0 0; color: #ffcccc; font-size: 14px;'>Equipment Borrowing Tracker System</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='margin: 0 0 20px 0; color: #333333; font-size: 22px;'>Borrowing Transaction Confirmed</h2>
                            <p style='margin: 0 0 15px 0; color: #555555;'>Hello <strong>{studentName}</strong>,</p>
                            <p style='margin: 0 0 25px 0; color: #555555;'>Your borrowing transaction has been successfully processed. Please keep this transaction code for your reference.</p>
                            
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                <tr>
                                    <td style='padding: 20px; background-color: #f8f9fa; border: 2px solid #8B0000; border-radius: 8px; text-align: center;'>
                                        <p style='margin: 0 0 5px 0; color: #6b7280; font-size: 12px; text-transform: uppercase; letter-spacing: 1px;'>Transaction Code</p>
                                        <p style='margin: 0; color: #8B0000; font-size: 24px; font-weight: bold; font-family: Consolas, monospace; letter-spacing: 2px;'>{transactionCode}</p>
                                    </td>
                                </tr>
                            </table>
                            
                            <div style='margin: 25px 0; padding: 20px; background-color: #f9f9f9; border-radius: 6px; border-left: 4px solid #8B0000;'>
                                <p style='margin: 0 0 10px 0; color: #333333; font-weight: bold;'>Transaction Details:</p>
                                <p style='margin: 0 0 5px 0; color: #555555;'><strong>Date Borrowed:</strong> {dateBorrowed:MMMM dd, yyyy 'at' hh:mm tt}</p>
                                <p style='margin: 15px 0 5px 0; color: #333333; font-weight: bold;'>Borrowed Items:</p>
                                <div style='color: #555555; white-space: pre-line;'>{itemsList}</div>
                            </div>
                            
                            <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0'>
                                <tr>
                                    <td style='padding: 15px; background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 6px;'>
                                        <p style='margin: 0; color: #856404; font-size: 14px;'><strong>⚠️ Important:</strong> Please return the borrowed items on or before the due date to avoid penalties.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #f9f9f9; padding: 25px 40px; text-align: center; border-top: 1px solid #eeeeee;'>
                            <p style='margin: 0 0 5px 0; color: #888888; font-size: 12px;'>© 2025 SupremeShare - Cebu Technological University Main Campus</p>
                            <p style='margin: 0; color: #aaaaaa; font-size: 11px;'>This is an automated message. Please do not reply.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword),
                    DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send transaction email to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Internal method to update overdue statuses - called automatically before fetching data
        /// </summary>
        private async Task UpdateOverdueStatusesInternal()
        {
            try
            {
                await _db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update overdue statuses");
                // Don't throw - this is a background operation
            }
        }

        // GET: SupportAdmin/Transactions/Stats
        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                // Auto-update overdue statuses before fetching stats
                await UpdateOverdueStatusesInternal();

                // Use database function fn_count_borrow_status for each status
                var activeBorrows = await _borrowRepo.CountByStatusAsync("borrowed");
                var overdueCount = await _borrowRepo.CountByStatusAsync("overdue");
                var lostCount = await _borrowRepo.CountByStatusAsync("lost");

                // Get today's stats
                var todayQuery = @"
                    SELECT 
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned' 
                         AND DATE(borrow_date_returned) = CURRENT_DATE) as returned_today,
                        (SELECT COUNT(*) FROM transaction WHERE DATE(transaction_date_borrowed) = CURRENT_DATE) as borrowed_today";

                var dt = await _db.ExecuteQueryAsync(todayQuery);
                var returnedToday = 0;
                var borrowedToday = 0;
                
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    returnedToday = Convert.ToInt32(row["returned_today"]);
                    borrowedToday = Convert.ToInt32(row["borrowed_today"]);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ActiveBorrows = activeBorrows,
                        OverdueCount = overdueCount,
                        ReturnedToday = returnedToday,
                        BorrowedToday = borrowedToday,
                        LostCount = lostCount
                    }
                });
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
