using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [AdminOnly]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            
            return View();
        }
    }
}
