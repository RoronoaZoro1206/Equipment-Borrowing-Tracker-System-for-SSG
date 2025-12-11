using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly IDashboardDataService _dashboardService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IDashboardDataService dashboardService, ILogger<AdminController> logger)
        {
            _dashboardService = dashboardService;
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
            var result = await _dashboardService.GetAuditLogsAsync(page, pageSize, action);

            return Json(new
            {
                success = true,
                data = result.Data.Select(a => new
                {
                    AuditId = a.AuditId,
                    Action = a.Action,
                    OldData = a.OldData,
                    NewData = a.NewData,
                    LoggedAt = a.LoggedAt,
                    Username = a.Username,
                    UserFullName = a.UserFullName
                }),
                pagination = new
                {
                    page = result.Pagination.Page,
                    pageSize = result.Pagination.PageSize,
                    totalCount = result.Pagination.TotalCount,
                    totalPages = result.Pagination.TotalPages
                }
            });
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            var stats = await _dashboardService.GetAdminStatsAsync();
            ViewBag.BorrowedCount = stats.Borrowed;
            ViewBag.AvailableCount = stats.Available;
            ViewBag.ForReviewCount = stats.ForReview;
            ViewBag.MaintenanceCount = stats.Maintenance;
            ViewBag.LostCount = stats.Lost;

            return View();
        }
    }
}
