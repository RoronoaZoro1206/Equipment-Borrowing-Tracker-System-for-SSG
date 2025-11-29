using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Equipment")]
    [AdminOnly]
    public class AdminEquipmentController : Controller
    {
        private readonly IEquipmentRepository _equipmentRepo;
        private readonly IEquipmentUnitRepository _unitRepo;
        private readonly ILogger<AdminEquipmentController> _logger;

        public AdminEquipmentController(
            IEquipmentRepository equipmentRepo,
            IEquipmentUnitRepository unitRepo,
            ILogger<AdminEquipmentController> logger)
        {
            _equipmentRepo = equipmentRepo;
            _unitRepo = unitRepo;
            _logger = logger;
        }

        // GET: Admin/Equipment
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var equipment = await _equipmentRepo.GetAllAsync();
            return View("~/Views/Admin/Equipment/Index.cshtml", equipment);
        }

        #region Equipment Type CRUD

        // GET: Admin/Equipment/GetAll
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

        // GET: Admin/Equipment/Get/{id}
        [HttpGet("Get/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var equipment = await _equipmentRepo.GetByIdAsync(id);
                if (equipment == null)
                    return Json(new { success = false, message = "Equipment not found" });

                return Json(new { success = true, data = equipment });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment {EquipmentId}", id);
                return Json(new { success = false, message = "Failed to fetch equipment" });
            }
        }

        // POST: Admin/Equipment/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] EquipmentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Json(new { success = false, message = "Equipment name is required" });
                if (string.IsNullOrWhiteSpace(request.Category))
                    return Json(new { success = false, message = "Category is required" });
                if (request.Quantity < 0)
                    return Json(new { success = false, message = "Quantity cannot be negative" });

                var equipment = new Equipment
                {
                    EquipmentName = request.Name,
                    EquipmentCategory = request.Category,
                    EquipmentQuantity = request.Quantity
                };

                var result = await _equipmentRepo.AddAsync(equipment);
                if (result)
                {
                    _logger.LogInformation("Equipment created: {Name} by user {UserId}",
                        request.Name, HttpContext.Session.GetInt32("UserId"));
                    return Json(new { success = true, message = "Equipment created successfully" });
                }

                return Json(new { success = false, message = "Failed to create equipment" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating equipment");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] EquipmentRequest request)
        {
            try
            {
                if (request.EquipmentId <= 0)
                    return Json(new { success = false, message = "Invalid equipment ID" });

                var equipment = await _equipmentRepo.GetByIdAsync(request.EquipmentId);
                if (equipment == null)
                    return Json(new { success = false, message = "Equipment not found" });

                equipment.EquipmentName = request.Name ?? equipment.EquipmentName;
                equipment.EquipmentCategory = request.Category ?? equipment.EquipmentCategory;
                equipment.EquipmentQuantity = request.Quantity;

                var result = await _equipmentRepo.UpdateAsync(equipment);
                return Json(new { success = result, message = result ? "Equipment updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating equipment");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/Delete
        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int equipmentId)
        {
            try
            {
                var result = await _equipmentRepo.DeleteAsync(equipmentId);
                return Json(new { success = result, message = result ? "Equipment deleted" : "Delete failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting equipment");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Equipment Units CRUD

        // GET: Admin/Equipment/Units/{equipmentId}
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

        // GET: Admin/Equipment/Unit/{id}
        [HttpGet("Unit/{id}")]
        public async Task<IActionResult> GetUnit(int id)
        {
            try
            {
                var unit = await _unitRepo.GetByIdAsync(id);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                return Json(new { success = true, data = unit });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unit {UnitId}", id);
                return Json(new { success = false, message = "Failed to fetch unit" });
            }
        }

        // GET: Admin/Equipment/AvailableUnits/{equipmentId}
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

        // POST: Admin/Equipment/CreateUnit
        [HttpPost("CreateUnit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUnit([FromForm] UnitRequest request)
        {
            try
            {
                if (request.EquipmentId <= 0)
                    return Json(new { success = false, message = "Equipment ID is required" });
                if (string.IsNullOrWhiteSpace(request.SerialNo))
                    return Json(new { success = false, message = "Serial number is required" });

                // Check if serial number exists
                var existing = await _unitRepo.GetBySerialNoAsync(request.SerialNo);
                if (existing != null)
                    return Json(new { success = false, message = "Serial number already exists" });

                var unit = new EquipmentUnit
                {
                    EquipmentId = request.EquipmentId,
                    EunitSerialNo = request.SerialNo,
                    EunitCondition = request.Condition, // true = Good, false = Damaged
                    EunitStatus = request.Status ?? "available"
                };

                var result = await _unitRepo.AddAsync(unit);
                if (result)
                {
                    _logger.LogInformation("Equipment unit created: {SerialNo} by user {UserId}",
                        request.SerialNo, HttpContext.Session.GetInt32("UserId"));
                    return Json(new { success = true, message = "Unit created successfully" });
                }

                return Json(new { success = false, message = "Failed to create unit" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/UpdateUnit
        [HttpPost("UpdateUnit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUnit([FromForm] UnitRequest request)
        {
            try
            {
                if (request.UnitId <= 0)
                    return Json(new { success = false, message = "Invalid unit ID" });

                var unit = await _unitRepo.GetByIdAsync(request.UnitId);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                // Check if new serial number conflicts
                if (!string.IsNullOrEmpty(request.SerialNo) && request.SerialNo != unit.EunitSerialNo)
                {
                    var existing = await _unitRepo.GetBySerialNoAsync(request.SerialNo);
                    if (existing != null && existing.EunitId != request.UnitId)
                        return Json(new { success = false, message = "Serial number already in use" });
                }

                unit.EunitSerialNo = request.SerialNo ?? unit.EunitSerialNo;
                unit.EunitCondition = request.Condition;
                unit.EunitStatus = request.Status ?? unit.EunitStatus;

                var result = await _unitRepo.UpdateAsync(unit);
                return Json(new { success = result, message = result ? "Unit updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/UpdateUnitCondition
        [HttpPost("UpdateUnitCondition")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUnitCondition([FromForm] int unitId, [FromForm] bool condition)
        {
            try
            {
                var unit = await _unitRepo.GetByIdAsync(unitId);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                unit.EunitCondition = condition;
                var result = await _unitRepo.UpdateAsync(unit);

                var conditionText = condition ? "Good" : "Damaged";
                _logger.LogInformation("Unit {UnitId} condition updated to {Condition} by user {UserId}",
                    unitId, conditionText, HttpContext.Session.GetInt32("UserId"));

                return Json(new { success = result, message = $"Condition updated to {conditionText}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit condition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/UpdateUnitStatus
        [HttpPost("UpdateUnitStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUnitStatus([FromForm] int unitId, [FromForm] string status)
        {
            try
            {
                var validStatuses = new[] { "available", "borrowed", "maintenance", "retired" };
                if (!validStatuses.Contains(status.ToLower()))
                    return Json(new { success = false, message = "Invalid status" });

                var unit = await _unitRepo.GetByIdAsync(unitId);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                unit.EunitStatus = status.ToLower();
                var result = await _unitRepo.UpdateAsync(unit);

                _logger.LogInformation("Unit {UnitId} status updated to {Status} by user {UserId}",
                    unitId, status, HttpContext.Session.GetInt32("UserId"));

                return Json(new { success = result, message = $"Status updated to {status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Equipment/DeleteUnit
        [HttpPost("DeleteUnit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnit([FromForm] int unitId)
        {
            try
            {
                var result = await _unitRepo.DeleteAsync(unitId);
                return Json(new { success = result, message = result ? "Unit deleted" : "Delete failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unit");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Dashboard Stats

        // GET: Admin/Equipment/Stats
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
                    DamagedUnits = allUnits.Count(u => !u.EunitCondition),
                    GoodConditionUnits = allUnits.Count(u => u.EunitCondition)
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment stats");
                return Json(new { success = false, message = "Failed to fetch stats" });
            }
        }

        #endregion
    }

    public class EquipmentRequest
    {
        public int EquipmentId { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public int Quantity { get; set; }
    }

    public class UnitRequest
    {
        public int UnitId { get; set; }
        public int EquipmentId { get; set; }
        public string? SerialNo { get; set; }
        public bool Condition { get; set; } = true; // true = Good
        public string? Status { get; set; }
    }
}
