using Microsoft.AspNetCore.Mvc;
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
    }
}
