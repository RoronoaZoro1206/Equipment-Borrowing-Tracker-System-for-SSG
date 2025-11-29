using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [Route("SupportAdmin/Equipment")]
    [SupportAdmin]
    public class SupportAdminEquipmentController : Controller
    {
        private readonly IEquipmentRepository _equipmentRepo;
        private readonly IEquipmentUnitRepository _unitRepo;
        private readonly ILogger<SupportAdminEquipmentController> _logger;

        public SupportAdminEquipmentController(
            IEquipmentRepository equipmentRepo,
            IEquipmentUnitRepository unitRepo,
            ILogger<SupportAdminEquipmentController> logger)
        {
            _equipmentRepo = equipmentRepo;
            _unitRepo = unitRepo;
            _logger = logger;
        }

        // GET: SupportAdmin/Equipment (View-Only)
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var equipment = await _equipmentRepo.GetAllAsync();
            return View("~/Views/SupportAdmin/Equipment/Index.cshtml", equipment);
        }

        // GET: SupportAdmin/Equipment/GetAll (API)
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var equipment = await _equipmentRepo.GetAllAsync();
                var result = new List<object>();

                foreach (var e in equipment)
                {
                    var availableStock = await _equipmentRepo.GetAvailableStockAsync(e.EquipmentId);
                    result.Add(new
                    {
                        e.EquipmentId,
                        e.EquipmentName,
                        e.EquipmentCategory,
                        e.EquipmentQuantity,
                        AvailableStock = availableStock,
                        EquipmentDateAdded = e.EquipmentDateAdded.ToString("yyyy-MM-dd")
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment");
                return Json(new { success = false, message = "Failed to fetch equipment" });
            }
        }


        // GET: SupportAdmin/Equipment/Units/{equipmentId}
        [HttpGet("Units/{equipmentId}")]
        public async Task<IActionResult> GetUnits(int equipmentId)
        {
            try
            {
                var units = await _unitRepo.GetByEquipmentIdAsync(equipmentId);
                var result = units.Select(u => new
                {
                    u.EunitId,
                    u.EquipmentId,
                    u.EunitSerialNo,
                    Condition = u.EunitCondition ? "Good" : "Damaged",
                    ConditionBool = u.EunitCondition,
                    u.EunitStatus,
                    EunitDateAdded = u.EunitDateAdded.ToString("yyyy-MM-dd")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching units for equipment {EquipmentId}", equipmentId);
                return Json(new { success = false, message = "Failed to fetch units" });
            }
        }

        // GET: SupportAdmin/Equipment/AvailableUnits/{equipmentId}
        [HttpGet("AvailableUnits/{equipmentId}")]
        public async Task<IActionResult> GetAvailableUnits(int equipmentId)
        {
            try
            {
                var units = await _unitRepo.GetAvailableUnitsAsync(equipmentId);
                var result = units.Select(u => new
                {
                    u.EunitId,
                    u.EunitSerialNo,
                    Condition = u.EunitCondition ? "Good" : "Damaged"
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available units");
                return Json(new { success = false, message = "Failed to fetch available units" });
            }
        }

        // POST: SupportAdmin/Equipment/UpdateUnitStatus (Limited to available/maintenance only)
        [HttpPost("UpdateUnitStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUnitStatus([FromForm] int unitId, [FromForm] string status)
        {
            try
            {
                // Support Admin can only set to 'available' or 'maintenance'
                var validStatuses = new[] { "available", "maintenance" };
                if (!validStatuses.Contains(status.ToLower()))
                    return Json(new { success = false, message = "Support Admin can only set status to 'available' or 'maintenance'" });

                var unit = await _unitRepo.GetByIdAsync(unitId);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                // Cannot change status of borrowed or lost items
                if (unit.EunitStatus == "borrowed" || unit.EunitStatus == "lost")
                    return Json(new { success = false, message = $"Cannot change status of {unit.EunitStatus} items. Contact Admin." });

                unit.EunitStatus = status.ToLower();
                var result = await _unitRepo.UpdateAsync(unit);

                var userId = HttpContext.Session.GetInt32("UserId") ?? 1;
                _logger.LogInformation("Unit {UnitId} status updated to {Status} by support admin {UserId}",
                    unitId, status, userId);

                return Json(new { success = result, message = $"Status updated to {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: SupportAdmin/Equipment/Stats
        [HttpGet("Stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var allEquipment = await _equipmentRepo.GetAllAsync();
                var allUnits = await _unitRepo.GetAllAsync();

                var stats = new
                {
                    TotalEquipmentTypes = allEquipment.Count(),
                    TotalUnits = allUnits.Count(),
                    AvailableUnits = allUnits.Count(u => u.EunitStatus == "available"),
                    BorrowedUnits = allUnits.Count(u => u.EunitStatus == "borrowed"),
                    MaintenanceUnits = allUnits.Count(u => u.EunitStatus == "maintenance"),
                    DamagedUnits = allUnits.Count(u => !u.EunitCondition)
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment stats");
                return Json(new { success = false, message = "Failed to fetch stats" });
            }
        }
    }
}
