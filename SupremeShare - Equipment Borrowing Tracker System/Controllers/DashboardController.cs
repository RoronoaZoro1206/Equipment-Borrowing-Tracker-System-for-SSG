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
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            
            // Redirect Support Admin to their dedicated dashboard
            if (role == "support_admin")
            {
                return RedirectToAction("Index", "SupportAdmin");
            }
            
            // Redirect Admin to their dashboard
            if (role == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = role;
            return View();
        }
    }
}
