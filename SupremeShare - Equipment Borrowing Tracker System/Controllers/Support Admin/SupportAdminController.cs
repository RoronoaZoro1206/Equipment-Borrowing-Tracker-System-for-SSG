using System.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [SupportAdmin]
    [Route("SupportAdmin")]
    public class SupportAdminController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<SupportAdminController> _logger;

        public SupportAdminController(DatabaseHelper db, ILogger<SupportAdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            try
            {
                // Auto-update overdue statuses
                await _db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()");
                
                // Load dashboard stats
                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'borrowed') as borrowed_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned' 
                         AND DATE(borrow_date_returned) = CURRENT_DATE) as returned_today,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'overdue') as overdue_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'lost') as lost_count";

                var dt = await _db.ExecuteQueryAsync(query);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    ViewBag.BorrowedCount = Convert.ToInt32(row["borrowed_count"]);
                    ViewBag.ReturnedToday = Convert.ToInt32(row["returned_today"]);
                    ViewBag.OverdueCount = Convert.ToInt32(row["overdue_count"]);
                    ViewBag.LostCount = Convert.ToInt32(row["lost_count"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                ViewBag.BorrowedCount = 0;
                ViewBag.ReturnedToday = 0;
                ViewBag.OverdueCount = 0;
                ViewBag.LostCount = 0;
            }

            return View();
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetStats - Get dashboard statistics
        /// </summary>
        [HttpGet("Dashboard/GetStats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                // Auto-update overdue statuses
                try { await _db.ExecuteNonQueryAsync("CALL sp_update_overdue_statuses()"); } catch { }
                
                var query = @"
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
                    return Json(new { 
                        success = true, 
                        data = new {
                            borrowed = Convert.ToInt32(row["borrowed"]),
                            returnedToday = Convert.ToInt32(row["returned_today"]),
                            overdue = Convert.ToInt32(row["overdue"]),
                            lost = Convert.ToInt32(row["lost"])
                        }
                    });
                }
                return Json(new { success = true, data = new { borrowed = 0, returnedToday = 0, overdue = 0, lost = 0 } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { success = false, message = "Failed to load stats" });
            }
        }

        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetRecordsByStatus - Get borrow records by status for modal display
        /// </summary>
        [HttpGet("Dashboard/GetRecordsByStatus")]
        public async Task<IActionResult> GetRecordsByStatus(string status)
        {
            try
            {
                if (string.IsNullOrEmpty(status))
                    return Json(new { success = false, message = "Status is required" });

                // Handle "returned" status specially - only today's returns
                var statusCondition = status.ToLower() == "returned" 
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

                var parameters = status.ToLower() == "returned" 
                    ? Array.Empty<NpgsqlParameter>() 
                    : new[] { new NpgsqlParameter("@status", status) };
                    
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                var records = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    records.Add(new
                    {
                        transactionId = Convert.ToInt32(row["transaction_id"]),
                        transactionCode = row["transaction_code"]?.ToString(),
                        dateBorrowed = row["date_borrowed"] != DBNull.Value ? Convert.ToDateTime(row["date_borrowed"]) : (DateTime?)null,
                        studentIdNumber = row["student_id_number"]?.ToString(),
                        studentName = row["student_name"]?.ToString(),
                        studentCollege = row["student_college"]?.ToString(),
                        studentCourse = row["student_course"]?.ToString(),
                        equipmentName = row["equipment_name"]?.ToString(),
                        equipmentCategory = row["equipment_category"]?.ToString(),
                        serialNo = row["serial_no"]?.ToString(),
                        borrowId = Convert.ToInt32(row["borrow_id"]),
                        status = row["borrow_status"]?.ToString(),
                        dueDate = row["borrow_due_date"] != DBNull.Value ? Convert.ToDateTime(row["borrow_due_date"]) : (DateTime?)null,
                        dateReturned = row["borrow_date_returned"] != DBNull.Value ? Convert.ToDateTime(row["borrow_date_returned"]) : (DateTime?)null
                    });
                }

                return Json(new { success = true, data = records });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting records by status: {Status}", status);
                return Json(new { success = false, message = "Failed to load records" });
            }
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetTransactionDetails - Get full transaction details for modal
        /// </summary>
        [HttpGet("Dashboard/GetTransactionDetails")]
        public async Task<IActionResult> GetTransactionDetails(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return Json(new { success = false, message = "Transaction code is required" });

                // Get transaction and student info
                var txnQuery = @"
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

                var txnDt = await _db.ExecuteQueryAsync(txnQuery, new[] { new NpgsqlParameter("@code", code) });
                
                if (txnDt.Rows.Count == 0)
                    return Json(new { success = false, message = "Transaction not found" });

                var txnRow = txnDt.Rows[0];
                var transactionId = Convert.ToInt32(txnRow["transaction_id"]);

                // Get borrow records (items)
                var itemsQuery = @"
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

                var itemsDt = await _db.ExecuteQueryAsync(itemsQuery, new[] { new NpgsqlParameter("@txnId", transactionId) });

                var items = new List<object>();
                foreach (DataRow row in itemsDt.Rows)
                {
                    items.Add(new
                    {
                        category = row["equipment_category"]?.ToString(),
                        equipment = row["equipment_name"]?.ToString(),
                        serialNo = row["eunit_serial_no"]?.ToString(),
                        status = row["borrow_status"]?.ToString(),
                        dateBorrowed = row["transaction_date_borrowed"] != DBNull.Value ? Convert.ToDateTime(row["transaction_date_borrowed"]) : (DateTime?)null,
                        dueDate = row["borrow_due_date"] != DBNull.Value ? Convert.ToDateTime(row["borrow_due_date"]) : (DateTime?)null,
                        dateReturned = row["borrow_date_returned"] != DBNull.Value ? Convert.ToDateTime(row["borrow_date_returned"]) : (DateTime?)null,
                        returnCondition = row["borrow_return_condition"]?.ToString(),
                        lastUpdated = row["borrow_last_updated"] != DBNull.Value ? Convert.ToDateTime(row["borrow_last_updated"]) : (DateTime?)null
                    });
                }

                // Get timeline from audit log
                var timelineQuery = @"
                    SELECT 
                        al.audit_logged_at as date, 
                        al.audit_action as action, 
                        COALESCE(CONCAT(u.user_first_name, ' ', u.user_last_name), 'System') as performed_by
                    FROM audit_log al
                    LEFT JOIN ""USER"" u ON al.user_id = u.user_id
                    WHERE al.audit_new_data LIKE @txnCode OR al.audit_old_data LIKE @txnCode
                    ORDER BY al.audit_logged_at DESC
                    LIMIT 20";
                    
                var timelineDt = await _db.ExecuteQueryAsync(timelineQuery, 
                    new[] { new NpgsqlParameter("@txnCode", $"%{code}%") });

                var timeline = new List<object>();
                foreach (DataRow row in timelineDt.Rows)
                {
                    timeline.Add(new
                    {
                        date = row["date"] != DBNull.Value ? Convert.ToDateTime(row["date"]) : (DateTime?)null,
                        action = row["action"]?.ToString(),
                        by = row["performed_by"]?.ToString()
                    });
                }

                // Add default entry if no timeline
                if (timeline.Count == 0)
                {
                    timeline.Add(new
                    {
                        date = txnRow["transaction_date_borrowed"] != DBNull.Value 
                            ? Convert.ToDateTime(txnRow["transaction_date_borrowed"]) : (DateTime?)null,
                        action = "Borrowing Transaction Recorded",
                        by = txnRow["recorded_by"]?.ToString()
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        transactionCode = txnRow["transaction_code"]?.ToString(),
                        dateBorrowed = txnRow["transaction_date_borrowed"] != DBNull.Value 
                            ? Convert.ToDateTime(txnRow["transaction_date_borrowed"]) : (DateTime?)null,
                        remarks = txnRow["transaction_remarks"]?.ToString(),
                        lastUpdated = txnRow["transaction_last_updated"] != DBNull.Value 
                            ? Convert.ToDateTime(txnRow["transaction_last_updated"]) : (DateTime?)null,
                        recordedBy = txnRow["recorded_by"]?.ToString(),
                        student = new
                        {
                            idNumber = txnRow["student_id_number"]?.ToString(),
                            name = txnRow["student_name"]?.ToString(),
                            college = txnRow["student_college"]?.ToString(),
                            course = txnRow["student_course"]?.ToString(),
                            email = txnRow["student_email"]?.ToString(),
                            contactNo = txnRow["student_contact_no"]?.ToString(),
                            status = txnRow["student_status"]?.ToString()
                        },
                        items,
                        timeline
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction details: {Code}", code);
                return Json(new { success = false, message = "Failed to load transaction details" });
            }
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetChartData - Get chart data for dashboard
        /// </summary>
        [HttpGet("Dashboard/GetChartData")]
        public async Task<IActionResult> GetChartData()
        {
            try
            {
                // Borrowers per College
                var collegeQuery = @"
                    SELECT s.student_college, COUNT(DISTINCT t.student_id) as borrower_count
                    FROM transaction t
                    JOIN student s ON t.student_id = s.student_id
                    WHERE s.student_college IS NOT NULL AND s.student_college != ''
                    GROUP BY s.student_college
                    ORDER BY borrower_count DESC
                    LIMIT 10";

                // Equipment per Category
                var categoryQuery = @"
                    SELECT equipment_category, COUNT(*) as unit_count
                    FROM equipment
                    WHERE equipment_category IS NOT NULL AND equipment_category != ''
                    GROUP BY equipment_category
                    ORDER BY unit_count DESC
                    LIMIT 10";

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
                        collegeData = collegeDt.AsEnumerable().Select(r => new { 
                            label = r["student_college"]?.ToString() ?? "Unknown", 
                            value = Convert.ToInt32(r["borrower_count"]) 
                        }),
                        categoryData = categoryDt.AsEnumerable().Select(r => new { 
                            label = r["equipment_category"]?.ToString() ?? "Unknown", 
                            value = Convert.ToInt32(r["unit_count"]) 
                        }),
                        conditionData = conditionDt.Rows.Count > 0 ? new { 
                            good = Convert.ToInt32(conditionDt.Rows[0]["good_count"]), 
                            damaged = Convert.ToInt32(conditionDt.Rows[0]["damaged_count"]) 
                        } : new { good = 0, damaged = 0 },
                        monthlyBorrows = monthlyDt.AsEnumerable().Select(r => new { 
                            month = r["month"]?.ToString(), 
                            count = Convert.ToInt32(r["borrow_count"]) 
                        }),
                        monthlyReturns = monthlyReturnDt.AsEnumerable().Select(r => new { 
                            month = r["month"]?.ToString(), 
                            count = Convert.ToInt32(r["return_count"]) 
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data");
                return Json(new { success = false, message = "Failed to load chart data" });
            }
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetRecentActivity - Get recent transaction activity
        /// </summary>
        [HttpGet("Dashboard/GetRecentActivity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var query = @"
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
                
                var activities = new List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    activities.Add(new
                    {
                        transactionCode = row["transaction_code"]?.ToString(),
                        timestamp = row["transaction_date_borrowed"] != DBNull.Value 
                            ? Convert.ToDateTime(row["transaction_date_borrowed"]) : (DateTime?)null,
                        studentName = row["student_name"]?.ToString(),
                        actionType = row["action_type"]?.ToString(),
                        equipmentName = row["equipment_name"]?.ToString()
                    });
                }

                return Json(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent activity");
                return Json(new { success = false, message = "Failed to load recent activity" });
            }
        }
    }
}
