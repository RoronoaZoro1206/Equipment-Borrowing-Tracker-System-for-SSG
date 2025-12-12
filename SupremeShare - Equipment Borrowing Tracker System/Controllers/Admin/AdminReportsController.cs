using System.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Reports")]
    [AdminOnly]
    public class AdminReportsController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ILogger<AdminReportsController> _logger;

        public AdminReportsController(
            DatabaseHelper db,
            IAuditLogRepository auditRepo,
            ILogger<AdminReportsController> logger)
        {
            _db = db;
            _auditRepo = auditRepo;
            _logger = logger;
        }

        // GET: Admin/Reports
        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Reports/Index.cshtml");
        }

        // GET: Admin/Reports/Overdue
        [HttpGet("Overdue")]
        public IActionResult Overdue()
        {
            return View("~/Views/Admin/Reports/Overdue.cshtml");
        }

        // GET: Admin/Reports/Equipment
        [HttpGet("Equipment")]
        public IActionResult Equipment()
        {
            return View("~/Views/Admin/Reports/Equipment.cshtml");
        }

        // GET: Admin/Reports/StudentHistory
        [HttpGet("StudentHistory")]
        public IActionResult StudentHistory()
        {
            return View("~/Views/Admin/Reports/StudentHistory.cshtml");
        }

        // GET: Admin/Reports/AuditLog
        [HttpGet("AuditLog")]
        public IActionResult AuditLog()
        {
            return View("~/Views/Admin/Reports/AuditLog.cshtml");
        }


        #region API Endpoints

        // GET: Admin/Reports/GetDashboardStats
        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'borrowed') as borrowed_count,
                        (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'available') as available_count,
                        (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'for review') as for_review_count,
                        (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'maintenance') as maintenance_count,
                        (SELECT COUNT(*) FROM equipment_unit WHERE eunit_status = 'lost') as lost_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'overdue') as overdue_count,
                        (SELECT COUNT(*) FROM transaction WHERE DATE(transaction_date_borrowed) = CURRENT_DATE) as today_borrowed,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned' AND DATE(borrow_date_returned) = CURRENT_DATE) as today_returned,
                        (SELECT COUNT(*) FROM student WHERE student_status = true) as active_students,
                        (SELECT COUNT(*) FROM equipment) as total_equipment_types,
                        (SELECT COUNT(*) FROM equipment_unit) as total_units";

                var dt = await _db.ExecuteQueryAsync(query);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            BorrowedCount = Convert.ToInt32(row["borrowed_count"]),
                            AvailableCount = Convert.ToInt32(row["available_count"]),
                            ForReviewCount = Convert.ToInt32(row["for_review_count"]),
                            MaintenanceCount = Convert.ToInt32(row["maintenance_count"]),
                            LostCount = Convert.ToInt32(row["lost_count"]),
                            OverdueCount = Convert.ToInt32(row["overdue_count"]),
                            TodayBorrowed = Convert.ToInt32(row["today_borrowed"]),
                            TodayReturned = Convert.ToInt32(row["today_returned"]),
                            ActiveStudents = Convert.ToInt32(row["active_students"]),
                            TotalEquipmentTypes = Convert.ToInt32(row["total_equipment_types"]),
                            TotalUnits = Convert.ToInt32(row["total_units"])
                        }
                    });
                }
                return Json(new { success = false, message = "No data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard stats");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Reports/GetOverdueItems
        [HttpGet("GetOverdueItems")]
        public async Task<IActionResult> GetOverdueItems()
        {
            try
            {
                var query = @"
                    SELECT b.borrow_id, b.borrow_due_date, b.borrow_status,
                           t.transaction_code, t.transaction_date_borrowed,
                           s.student_id, s.student_id_number, s.student_first_name, s.student_last_name,
                           s.student_college, s.student_email, s.student_contact_no,
                           eu.eunit_serial_no, e.equipment_name, e.equipment_category,
                           EXTRACT(DAY FROM (NOW() - b.borrow_due_date))::INT as days_overdue
                    FROM borrow_record b
                    JOIN transaction t ON b.transaction_id = t.transaction_id
                    JOIN student s ON t.student_id = s.student_id
                    JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                    JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE b.borrow_status IN ('borrowed', 'overdue') AND b.borrow_due_date < NOW()
                    ORDER BY b.borrow_due_date ASC";

                var dt = await _db.ExecuteQueryAsync(query);
                var result = dt.AsEnumerable().Select(row => new
                {
                    BorrowId = Convert.ToInt32(row["borrow_id"]),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    StudentIdNumber = row["student_id_number"]?.ToString(),
                    StudentName = $"{row["student_first_name"]} {row["student_last_name"]}",
                    StudentCollege = row["student_college"]?.ToString(),
                    StudentEmail = row["student_email"]?.ToString(),
                    StudentContact = row["student_contact_no"]?.ToString(),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    Category = row["equipment_category"]?.ToString(),
                    SerialNo = row["eunit_serial_no"]?.ToString(),
                    DueDate = Convert.ToDateTime(row["borrow_due_date"]).ToString("yyyy-MM-dd"),
                    DateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]).ToString("yyyy-MM-dd"),
                    DaysOverdue = Convert.ToInt32(row["days_overdue"]),
                    Status = row["borrow_status"]?.ToString()
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching overdue items");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Reports/GetEquipmentUtilization
        [HttpGet("GetEquipmentUtilization")]
        public async Task<IActionResult> GetEquipmentUtilization()
        {
            try
            {
                var query = @"
                    SELECT e.equipment_id, e.equipment_name, e.equipment_category,
                           COUNT(DISTINCT eu.eunit_id) as total_units,
                           COUNT(DISTINCT CASE WHEN eu.eunit_status = 'available' THEN eu.eunit_id END) as available_units,
                           COUNT(DISTINCT CASE WHEN eu.eunit_status = 'borrowed' THEN eu.eunit_id END) as borrowed_units,
                           COUNT(b.borrow_id) as total_borrows,
                           COUNT(CASE WHEN b.borrow_status = 'returned' THEN 1 END) as total_returns,
                           ROUND(COUNT(b.borrow_id)::DECIMAL / NULLIF(COUNT(DISTINCT eu.eunit_id), 0), 2) as utilization_rate
                    FROM equipment e
                    LEFT JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
                    LEFT JOIN borrow_record b ON eu.eunit_id = b.eunit_id
                    GROUP BY e.equipment_id, e.equipment_name, e.equipment_category
                    ORDER BY total_borrows DESC";

                var dt = await _db.ExecuteQueryAsync(query);
                var result = dt.AsEnumerable().Select(row => new
                {
                    EquipmentId = Convert.ToInt32(row["equipment_id"]),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    Category = row["equipment_category"]?.ToString(),
                    TotalUnits = Convert.ToInt32(row["total_units"]),
                    AvailableUnits = Convert.ToInt32(row["available_units"]),
                    BorrowedUnits = Convert.ToInt32(row["borrowed_units"]),
                    TotalBorrows = Convert.ToInt32(row["total_borrows"]),
                    TotalReturns = Convert.ToInt32(row["total_returns"]),
                    UtilizationRate = row["utilization_rate"] != DBNull.Value ? Convert.ToDecimal(row["utilization_rate"]) : 0
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment utilization");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Reports/GetStudentHistory/{studentId}
        [HttpGet("GetStudentHistory/{studentId}")]
        public async Task<IActionResult> GetStudentHistory(int studentId)
        {
            try
            {
                var query = @"
                    SELECT t.transaction_id, t.transaction_code, t.transaction_date_borrowed,
                           b.borrow_id, b.borrow_status, b.borrow_due_date, b.borrow_date_returned,
                           b.borrow_return_condition,
                           eu.eunit_serial_no, e.equipment_name, e.equipment_category
                    FROM transaction t
                    JOIN borrow_record b ON t.transaction_id = b.transaction_id
                    JOIN equipment_unit eu ON b.eunit_id = eu.eunit_id
                    JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE t.student_id = @student_id
                    ORDER BY t.transaction_date_borrowed DESC";

                var parameters = new NpgsqlParameter[] { new("@student_id", studentId) };
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                var result = dt.AsEnumerable().Select(row => new
                {
                    TransactionId = Convert.ToInt32(row["transaction_id"]),
                    TransactionCode = row["transaction_code"]?.ToString(),
                    DateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]).ToString("yyyy-MM-dd HH:mm"),
                    BorrowId = Convert.ToInt32(row["borrow_id"]),
                    Status = row["borrow_status"]?.ToString(),
                    DueDate = Convert.ToDateTime(row["borrow_due_date"]).ToString("yyyy-MM-dd"),
                    DateReturned = row["borrow_date_returned"] != DBNull.Value 
                        ? Convert.ToDateTime(row["borrow_date_returned"]).ToString("yyyy-MM-dd HH:mm") : null,
                    ReturnCondition = row["borrow_return_condition"] != DBNull.Value 
                        ? (Convert.ToBoolean(row["borrow_return_condition"]) ? "Good" : "Damaged") : null,
                    SerialNo = row["eunit_serial_no"]?.ToString(),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    Category = row["equipment_category"]?.ToString()
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student history");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Reports/GetAuditLogs
        [HttpGet("GetAuditLogs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? action = null)
        {
            try
            {
                var offset = (page - 1) * pageSize;
                var whereClause = string.IsNullOrEmpty(action) ? "" : "WHERE a.audit_action ILIKE @action";
                
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
                
                if (!string.IsNullOrEmpty(action))
                    parameters.Add(new("@action", $"%{action}%"));

                var dt = await _db.ExecuteQueryAsync(query, parameters.ToArray());

                var result = dt.AsEnumerable().Select(row => new
                {
                    AuditId = Convert.ToInt32(row["audit_id"]),
                    Action = row["audit_action"]?.ToString(),
                    OldData = row["audit_old_data"]?.ToString(),
                    NewData = row["audit_new_data"]?.ToString(),
                    LoggedAt = Convert.ToDateTime(row["audit_logged_at"]).ToString("yyyy-MM-dd HH:mm:ss"),
                    Username = row["user_username"]?.ToString(),
                    UserFullName = $"{row["user_first_name"]} {row["user_last_name"]}"
                });

                // Get total count
                var countQuery = string.IsNullOrEmpty(action) 
                    ? "SELECT COUNT(*) FROM audit_log"
                    : "SELECT COUNT(*) FROM audit_log WHERE audit_action ILIKE @action";
                var countParams = string.IsNullOrEmpty(action) 
                    ? Array.Empty<NpgsqlParameter>() 
                    : new[] { new NpgsqlParameter("@action", $"%{action}%") };
                var countDt = await _db.ExecuteQueryAsync(countQuery, countParams);
                var totalCount = Convert.ToInt32(countDt.Rows[0][0]);

                return Json(new { 
                    success = true, 
                    data = result, 
                    pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Reports/GetChartData
        [HttpGet("GetChartData")]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                // Borrowers per College
                var collegeQuery = @"
                    SELECT s.student_college, COUNT(DISTINCT t.student_id) as borrower_count
                    FROM transaction t
                    JOIN student s ON t.student_id = s.student_id
                    GROUP BY s.student_college
                    ORDER BY borrower_count DESC";

                // Equipment per Category - Fixed: Count actual units, not equipment types
                var categoryQuery = @"
                    SELECT e.equipment_category, COUNT(eu.eunit_id) as unit_count
                    FROM equipment e
                    LEFT JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
                    WHERE e.equipment_category IS NOT NULL AND e.equipment_category != ''
                    GROUP BY e.equipment_category
                    ORDER BY unit_count DESC";

                // Good vs Damaged
                var conditionQuery = @"
                    SELECT 
                        COUNT(CASE WHEN eunit_condition = true THEN 1 END) as good_count,
                        COUNT(CASE WHEN eunit_condition = false THEN 1 END) as damaged_count
                    FROM equipment_unit";

                // Monthly Transactions (last 12 months)
                var monthlyQuery = @"
                    SELECT 
                        TO_CHAR(transaction_date_borrowed, 'Mon') as month,
                        EXTRACT(MONTH FROM transaction_date_borrowed) as month_num,
                        COUNT(*) as borrow_count
                    FROM transaction
                    WHERE transaction_date_borrowed >= NOW() - INTERVAL '12 months'
                    GROUP BY TO_CHAR(transaction_date_borrowed, 'Mon'), EXTRACT(MONTH FROM transaction_date_borrowed)
                    ORDER BY month_num";

                var monthlyReturnQuery = @"
                    SELECT 
                        TO_CHAR(borrow_date_returned, 'Mon') as month,
                        EXTRACT(MONTH FROM borrow_date_returned) as month_num,
                        COUNT(*) as return_count
                    FROM borrow_record
                    WHERE borrow_date_returned IS NOT NULL AND borrow_date_returned >= NOW() - INTERVAL '12 months'
                    GROUP BY TO_CHAR(borrow_date_returned, 'Mon'), EXTRACT(MONTH FROM borrow_date_returned)
                    ORDER BY month_num";

                var collegeDt = await _db.ExecuteQueryAsync(collegeQuery);
                var categoryDt = await _db.ExecuteQueryAsync(categoryQuery);
                var conditionDt = await _db.ExecuteQueryAsync(conditionQuery);
                var monthlyDt = await _db.ExecuteQueryAsync(monthlyQuery);
                var monthlyReturnDt = await _db.ExecuteQueryAsync(monthlyReturnQuery);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        CollegeData = collegeDt.AsEnumerable().Select(r => new { 
                            Label = r["student_college"]?.ToString(), 
                            Value = Convert.ToInt32(r["borrower_count"]) 
                        }),
                        CategoryData = categoryDt.AsEnumerable().Select(r => new { 
                            Label = r["equipment_category"]?.ToString(), 
                            Value = Convert.ToInt32(r["unit_count"]) 
                        }),
                        ConditionData = conditionDt.Rows.Count > 0 ? new { 
                            Good = Convert.ToInt32(conditionDt.Rows[0]["good_count"]), 
                            Damaged = Convert.ToInt32(conditionDt.Rows[0]["damaged_count"]) 
                        } : new { Good = 0, Damaged = 0 },
                        MonthlyBorrows = monthlyDt.AsEnumerable().Select(r => new { 
                            Month = r["month"]?.ToString(), 
                            Count = Convert.ToInt32(r["borrow_count"]) 
                        }),
                        MonthlyReturns = monthlyReturnDt.AsEnumerable().Select(r => new { 
                            Month = r["month"]?.ToString(), 
                            Count = Convert.ToInt32(r["return_count"]) 
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}
