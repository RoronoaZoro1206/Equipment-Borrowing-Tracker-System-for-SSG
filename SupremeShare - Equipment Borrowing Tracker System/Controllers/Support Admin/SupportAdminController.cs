using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [SupportAdmin]
    public class SupportAdminController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<SupportAdminController> _logger;

        public SupportAdminController(DatabaseHelper db, ILogger<SupportAdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            try
            {
                // Load dashboard stats
                var query = @"
                    SELECT 
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'borrowed') as borrowed_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'returned') as returned_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'overdue') as overdue_count,
                        (SELECT COUNT(*) FROM borrow_record WHERE borrow_status = 'lost') as lost_count";

                var dt = await _db.ExecuteQueryAsync(query);
                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    ViewBag.BorrowedCount = Convert.ToInt32(row["borrowed_count"]);
                    ViewBag.ReturnedCount = Convert.ToInt32(row["returned_count"]);
                    ViewBag.OverdueCount = Convert.ToInt32(row["overdue_count"]);
                    ViewBag.LostCount = Convert.ToInt32(row["lost_count"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                ViewBag.BorrowedCount = 0;
                ViewBag.ReturnedCount = 0;
                ViewBag.OverdueCount = 0;
                ViewBag.LostCount = 0;
            }

            return View();
        }

        /// <summary>
        /// GET: /SupportAdmin/GetRecordsByStatus - Get borrow records by status for modal display
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecordsByStatus(string status)
        {
            try
            {
                if (string.IsNullOrEmpty(status))
                    return Json(new { success = false, message = "Status is required" });

                var query = @"
                    SELECT 
                        t.transaction_code,
                        t.transaction_date_borrowed as date_borrowed,
                        s.student_id_number,
                        CONCAT(s.student_first_name, ' ', s.student_last_name) as student_name,
                        e.equipment_name,
                        e.equipment_category,
                        eu.eunit_serial_no as serial_no,
                        br.borrow_status,
                        br.borrow_due_date,
                        br.borrow_date_returned
                    FROM borrow_record br
                    INNER JOIN transaction t ON br.transaction_id = t.transaction_id
                    INNER JOIN student s ON t.student_id = s.student_id
                    INNER JOIN equipment_unit eu ON br.eunit_id = eu.eunit_id
                    INNER JOIN equipment e ON eu.equipment_id = e.equipment_id
                    WHERE LOWER(br.borrow_status) = LOWER(@status)
                    ORDER BY t.transaction_date_borrowed DESC
                    LIMIT 100";

                var dt = await _db.ExecuteQueryAsync(query, new[] { new NpgsqlParameter("@status", status) });

                var records = new List<object>();
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    records.Add(new
                    {
                        transactionCode = row["transaction_code"]?.ToString(),
                        dateBorrowed = row["date_borrowed"] != DBNull.Value ? Convert.ToDateTime(row["date_borrowed"]) : (DateTime?)null,
                        studentIdNumber = row["student_id_number"]?.ToString(),
                        studentName = row["student_name"]?.ToString(),
                        equipmentName = row["equipment_name"]?.ToString(),
                        equipmentCategory = row["equipment_category"]?.ToString(),
                        serialNo = row["serial_no"]?.ToString(),
                        borrowStatus = row["borrow_status"]?.ToString(),
                        borrowDueDate = row["borrow_due_date"] != DBNull.Value ? Convert.ToDateTime(row["borrow_due_date"]) : (DateTime?)null,
                        borrowDateReturned = row["borrow_date_returned"] != DBNull.Value ? Convert.ToDateTime(row["borrow_date_returned"]) : (DateTime?)null
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
    }
}
