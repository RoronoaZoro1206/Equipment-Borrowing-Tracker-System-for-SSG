using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/BulkOperations")]
    [AdminOnly]
    public class AdminBulkOperationsController : Controller
    {
        private readonly IBulkOperationsService _bulkService;
        private readonly IExportService _exportService;
        private readonly ILostItemProtocolService _lostItemService;
        private readonly IEquipmentConditionTrackingService _conditionService;
        private readonly ILogger<AdminBulkOperationsController> _logger;

        public AdminBulkOperationsController(
            IBulkOperationsService bulkService,
            IExportService exportService,
            ILostItemProtocolService lostItemService,
            IEquipmentConditionTrackingService conditionService,
            ILogger<AdminBulkOperationsController> logger)
        {
            _bulkService = bulkService;
            _exportService = exportService;
            _lostItemService = lostItemService;
            _conditionService = conditionService;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/BulkOperations/Index.cshtml");
        }

        [HttpGet("BulkBorrow")]
        public IActionResult BulkBorrow()
        {
            return View("~/Views/Admin/BulkOperations/BulkBorrow.cshtml");
        }

        [HttpGet("BulkReturn")]
        public IActionResult BulkReturn()
        {
            return View("~/Views/Admin/BulkOperations/BulkReturn.cshtml");
        }

        [HttpGet("LostItems")]
        public async Task<IActionResult> LostItems()
        {
            var lostItems = await _lostItemService.GetLostItemsAsync();
            return View("~/Views/Admin/BulkOperations/LostItems.cshtml", lostItems);
        }

        [HttpGet("ConditionTracking")]
        public async Task<IActionResult> ConditionTracking()
        {
            var summary = await _conditionService.GetConditionSummaryAsync();
            return View("~/Views/Admin/BulkOperations/ConditionTracking.cshtml", summary);
        }

        [HttpPost("ProcessBulkBorrow")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBulkBorrow([FromBody] BulkBorrowRequest request)
        {
            try
            {
                request.UserId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var result = await _bulkService.BulkBorrowAsync(request);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk borrow");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("ProcessBulkReturn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBulkReturn([FromBody] BulkReturnRequest request)
        {
            try
            {
                request.UserId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var result = await _bulkService.BulkReturnAsync(request);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk return");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("MarkAsLost")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsLost([FromBody] MarkAsLostRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var result = await _lostItemService.MarkAsLostAsync(request.BorrowId, userId, request.Remarks);
                return Json(new { success = result.Success, message = result.Message, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking item as lost");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("UnblockStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockStudent([FromBody] UnblockStudentRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var result = await _lostItemService.UnblockStudentAsync(request.StudentId, userId, request.Reason);
                return Json(new { success = result, message = result ? "Student unblocked successfully" : "Failed to unblock student" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking student");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetConditionHistory/{unitId}")]
        public async Task<IActionResult> GetConditionHistory(int unitId)
        {
            try
            {
                var history = await _conditionService.GetConditionHistoryAsync(unitId);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting condition history");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("GetAllConditionChanges")]
        public async Task<IActionResult> GetAllConditionChanges([FromQuery] int limit = 100)
        {
            try
            {
                var history = await _conditionService.GetAllConditionChangesAsync(limit);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting condition changes");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("UpdateUnitCondition")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUnitCondition([FromBody] UpdateConditionRequest request)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                var result = await _conditionService.UpdateUnitConditionAsync(
                    request.UnitId, request.NewCondition, userId, request.Remarks);
                return Json(new { success = result, message = result ? "Condition updated successfully" : "Failed to update condition" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit condition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Export endpoints
        [HttpGet("ExportOverdueReport")]
        public async Task<IActionResult> ExportOverdueReport()
        {
            try
            {
                var data = await _exportService.ExportOverdueReportAsync();
                var fileName = $"overdue_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting overdue report");
                return BadRequest("Error generating report");
            }
        }

        [HttpGet("ExportEquipmentUtilization")]
        public async Task<IActionResult> ExportEquipmentUtilization()
        {
            try
            {
                var data = await _exportService.ExportEquipmentUtilizationAsync();
                var fileName = $"equipment_utilization_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting equipment utilization");
                return BadRequest("Error generating report");
            }
        }

        [HttpGet("ExportAuditLog")]
        public async Task<IActionResult> ExportAuditLog([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _exportService.ExportAuditLogAsync(fromDate, toDate);
                var fileName = $"audit_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit log");
                return BadRequest("Error generating report");
            }
        }

        [HttpGet("ExportStudentHistory/{studentId}")]
        public async Task<IActionResult> ExportStudentHistory(int studentId)
        {
            try
            {
                var data = await _exportService.ExportStudentHistoryAsync(studentId);
                var fileName = $"student_history_{studentId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting student history");
                return BadRequest("Error generating report");
            }
        }

        [HttpGet("ExportInventory")]
        public async Task<IActionResult> ExportInventory()
        {
            try
            {
                var data = await _exportService.ExportInventoryReportAsync();
                var fileName = $"inventory_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory");
                return BadRequest("Error generating report");
            }
        }
    }

    public class MarkAsLostRequest
    {
        public int BorrowId { get; set; }
        public string? Remarks { get; set; }
    }

    public class UnblockStudentRequest
    {
        public int StudentId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UpdateConditionRequest
    {
        public int UnitId { get; set; }
        public bool NewCondition { get; set; }
        public string? Remarks { get; set; }
    }
}
