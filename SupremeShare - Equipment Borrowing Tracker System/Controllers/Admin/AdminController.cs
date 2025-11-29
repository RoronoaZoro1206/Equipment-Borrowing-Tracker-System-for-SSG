using Microsoft.AspNetCore.Mvc;
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
