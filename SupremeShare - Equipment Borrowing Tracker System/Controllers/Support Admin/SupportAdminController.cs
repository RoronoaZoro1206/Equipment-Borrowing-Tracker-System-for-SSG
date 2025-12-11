using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [SupportAdmin]
    [Route("SupportAdmin")]
    public class SupportAdminController : Controller
    {
        private readonly IDashboardDataService _dashboardService;
        private readonly ILogger<SupportAdminController> _logger;

        public SupportAdminController(IDashboardDataService dashboardService, ILogger<SupportAdminController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewBag.FullName = HttpContext.Session.GetString("FullName");
            ViewBag.Role = HttpContext.Session.GetString("UserRole");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");

            var stats = await _dashboardService.GetSupportAdminStatsAsync();
            ViewBag.BorrowedCount = stats.Borrowed;
            ViewBag.ReturnedToday = stats.ReturnedToday;
            ViewBag.OverdueCount = stats.Overdue;
            ViewBag.LostCount = stats.Lost;

            return View();
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetStats - Get dashboard statistics
        /// </summary>
        [HttpGet("Dashboard/GetStats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetSupportAdminStatsAsync();
            return Json(new { 
                success = true, 
                data = new {
                    borrowed = stats.Borrowed,
                    returnedToday = stats.ReturnedToday,
                    overdue = stats.Overdue,
                    lost = stats.Lost
                }
            });
        }

        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetRecordsByStatus - Get borrow records by status for modal display
        /// </summary>
        [HttpGet("Dashboard/GetRecordsByStatus")]
        public async Task<IActionResult> GetRecordsByStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return Json(new { success = false, message = "Status is required" });

            var records = await _dashboardService.GetBorrowRecordsByStatusAsync(status);
            
            // Map to anonymous objects for JSON serialization (camelCase)
            var data = records.Select(r => new
            {
                transactionId = r.TransactionId,
                transactionCode = r.TransactionCode,
                dateBorrowed = r.DateBorrowed,
                studentIdNumber = r.StudentIdNumber,
                studentName = r.StudentName,
                studentCollege = r.StudentCollege,
                studentCourse = r.StudentCourse,
                equipmentName = r.EquipmentName,
                equipmentCategory = r.EquipmentCategory,
                serialNo = r.SerialNo,
                borrowId = r.BorrowId,
                status = r.Status,
                dueDate = r.DueDate,
                dateReturned = r.DateReturned
            });

            return Json(new { success = true, data });
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetTransactionDetails - Get full transaction details for modal
        /// </summary>
        [HttpGet("Dashboard/GetTransactionDetails")]
        public async Task<IActionResult> GetTransactionDetails(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { success = false, message = "Transaction code is required" });

            var details = await _dashboardService.GetTransactionDetailsAsync(code);
            
            if (details == null)
                return Json(new { success = false, message = "Transaction not found" });

            // Map to anonymous objects for JSON serialization (camelCase)
            return Json(new
            {
                success = true,
                data = new
                {
                    transactionCode = details.TransactionCode,
                    dateBorrowed = details.DateBorrowed,
                    remarks = details.Remarks,
                    lastUpdated = details.LastUpdated,
                    recordedBy = details.RecordedBy,
                    student = new
                    {
                        idNumber = details.Student.IdNumber,
                        name = details.Student.Name,
                        college = details.Student.College,
                        course = details.Student.Course,
                        email = details.Student.Email,
                        contactNo = details.Student.ContactNo,
                        status = details.Student.Status
                    },
                    items = details.Items.Select(i => new
                    {
                        category = i.Category,
                        equipment = i.Equipment,
                        serialNo = i.SerialNo,
                        status = i.Status,
                        dateBorrowed = i.DateBorrowed,
                        dueDate = i.DueDate,
                        dateReturned = i.DateReturned,
                        returnCondition = i.ReturnCondition,
                        lastUpdated = i.LastUpdated
                    }),
                    timeline = details.Timeline.Select(t => new
                    {
                        date = t.Date,
                        action = t.Action,
                        by = t.By
                    })
                }
            });
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetChartData - Get chart data for dashboard
        /// </summary>
        [HttpGet("Dashboard/GetChartData")]
        public async Task<IActionResult> GetChartData()
        {
            var chartData = await _dashboardService.GetChartDataAsync();

            return Json(new
            {
                success = true,
                data = new
                {
                    collegeData = chartData.CollegeData.Select(c => new { label = c.Label, value = c.Value }),
                    categoryData = chartData.CategoryData.Select(c => new { label = c.Label, value = c.Value }),
                    conditionData = new { good = chartData.ConditionData.Good, damaged = chartData.ConditionData.Damaged },
                    monthlyBorrows = chartData.MonthlyBorrows.Select(m => new { month = m.Month, count = m.Count }),
                    monthlyReturns = chartData.MonthlyReturns.Select(m => new { month = m.Month, count = m.Count })
                }
            });
        }
        
        /// <summary>
        /// GET: /SupportAdmin/Dashboard/GetRecentActivity - Get recent transaction activity
        /// </summary>
        [HttpGet("Dashboard/GetRecentActivity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var activities = await _dashboardService.GetRecentActivityAsync();

            var data = activities.Select(a => new
            {
                transactionCode = a.TransactionCode,
                timestamp = a.Timestamp,
                studentName = a.StudentName,
                actionType = a.ActionType,
                equipmentName = a.EquipmentName
            });

            return Json(new { success = true, data });
        }
    }
}
