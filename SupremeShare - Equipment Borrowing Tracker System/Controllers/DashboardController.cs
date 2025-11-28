using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers
{
    [AuthorizeRole("admin", "support_admin")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            return View();
        }
    }
}
