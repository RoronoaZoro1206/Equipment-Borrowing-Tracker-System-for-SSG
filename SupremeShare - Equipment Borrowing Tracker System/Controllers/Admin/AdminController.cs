using System.Data;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(DatabaseHelper db, ILogger<AdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: Admin/AuditLog
        [HttpGet]
        [Route("Admin/AuditLog")]
        public IActionResult AuditLog()
        {
            return View("~/Views/Admin/Reports/AuditLog.cshtml");
        }

        // GET: Admin/AuditLog/GetLogs
        [HttpGet]
        [Route("Admin/AuditLog/GetLogs")]
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

        public async Task<IActionResult> Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            try
            {
                var query = @"
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
                    ViewBag.BorrowedCount = Convert.ToInt32(row["borrowed_count"]);
                    ViewBag.AvailableCount = Convert.ToInt32(row["available_count"]);
                    ViewBag.ForReviewCount = Convert.ToInt32(row["for_review_count"]);
                    ViewBag.MaintenanceCount = Convert.ToInt32(row["maintenance_count"]);
                    ViewBag.LostCount = Convert.ToInt32(row["lost_count"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard stats");
                ViewBag.BorrowedCount = 0;
                ViewBag.AvailableCount = 0;
                ViewBag.ForReviewCount = 0;
                ViewBag.MaintenanceCount = 0;
                ViewBag.LostCount = 0;
            }

            return View();
        }
    }
}
