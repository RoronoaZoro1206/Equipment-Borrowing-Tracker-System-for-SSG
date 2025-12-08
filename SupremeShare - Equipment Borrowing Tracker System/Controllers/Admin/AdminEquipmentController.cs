using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Equipment")]
    [AdminOnly]
    public class AdminEquipmentController : Controller
    {
        private readonly IEquipmentRepository _equipmentRepo;
        private readonly IEquipmentUnitRepository _unitRepo;
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<AdminEquipmentController> _logger;

        public AdminEquipmentController(
            IEquipmentRepository equipmentRepo,
            IEquipmentUnitRepository unitRepo,
            DatabaseHelper db,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<AdminEquipmentController> logger)
        {
            _equipmentRepo = equipmentRepo;
            _unitRepo = unitRepo;
            _db = db;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Admin/Equipment
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var equipment = await _equipmentRepo.GetAllAsync();
            return View("~/Views/Admin/Equipment/Index.cshtml", equipment);
        }

        // GET: Admin/Equipment/Units - Equipment Units Management Page
        [HttpGet("Units")]
        public async Task<IActionResult> UnitsPage()
        {
            var equipment = await _equipmentRepo.GetAllAsync();
            return View("~/Views/Admin/Equipment/Units.cshtml", equipment);
        }

        // GET: Admin/Equipment/GetAllUnits - All units with equipment info for Units page
        [HttpGet("GetAllUnits")]
        public async Task<IActionResult> GetAllUnits()
        {
            try
            {
                var allUnits = await _unitRepo.GetAllAsync();
                var result = new List<object>();

                foreach (var unit in allUnits)
                {
                    var equipment = await _equipmentRepo.GetByIdAsync(unit.EquipmentId);
                    result.Add(new
                    {
                        unit.EunitId,
                        unit.EquipmentId,
                        EquipmentName = equipment?.EquipmentName ?? "Unknown",
                        EquipmentCategory = equipment?.EquipmentCategory ?? "Unknown",
                        unit.EunitSerialNo,
                        Condition = unit.EunitCondition ? "Good" : "Damaged",
                        ConditionBool = unit.EunitCondition,
                        unit.EunitStatus,
                        EunitDateAdded = unit.EunitDateAdded.ToString("yyyy-MM-dd"),
                        EunitLastUpdated = unit.EunitLastUpdated.ToString("yyyy-MM-dd HH:mm")
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all units");
                return Json(new { success = false, message = "Failed to fetch units" });
            }
        }

        #region Equipment Type CRUD

        // GET: Admin/Equipment/GetCategories (API)
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var equipment = await _equipmentRepo.GetAllAsync();
                var categories = equipment
                    .Select(e => e.EquipmentCategory)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment categories");
                return Json(new { success = false, message = "Failed to fetch categories" });
            }
        }

        // GET: Admin/Equipment/GetByCategory?category=xxx
        [HttpGet("GetByCategory")]
        public async Task<IActionResult> GetByCategory([FromQuery] string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    return Json(new { success = false, message = "Category is required" });

                var equipment = await _equipmentRepo.GetAllAsync();
                var filtered = equipment
                    .Where(e => e.EquipmentCategory.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .Select(e => new
                    {
                        e.EquipmentId,
                        e.EquipmentName,
                        e.EquipmentCategory
                    })
                    .ToList();

                return Json(new { success = true, data = filtered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching equipment by category {Category}", category);
                return Json(new { success = false, message = "Failed to fetch equipment" });
            }
        }

        // GET: Admin/Equipment/GetAvailableUnits?equipmentId=xxx
        [HttpGet("GetAvailableUnits")]
        public async Task<IActionResult> GetAvailableUnitsQuery([FromQuery] int equipmentId)
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
                _logger.LogError(ex, "Error fetching available units for equipment {EquipmentId}", equipmentId);
                return Json(new { success = false, message = "Failed to fetch available units" });
            }
        }

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
                    result.Add(new
                    {
                        e.EquipmentId,
                        e.EquipmentName,
                        e.EquipmentCategory,
                        e.EquipmentQuantity,
                        EquipmentDateAdded = e.EquipmentDateAdded.ToString("yyyy-MM-dd HH:mm"),
                        EquipmentLastUpdated = e.EquipmentLastUpdated.ToString("yyyy-MM-dd HH:mm")
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
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Equipment created: {Name} by user {UserId}", request.Name, userId);
                    
                    await _auditService.LogActionAsync(userId, "EQUIPMENT_CREATED", null, new { request.Name, request.Category });
                    await _notificationService.NotifyEquipmentUpdated(request.Name, "CREATED");
                    
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

                var oldName = equipment.EquipmentName;
                equipment.EquipmentName = request.Name ?? equipment.EquipmentName;
                equipment.EquipmentCategory = request.Category ?? equipment.EquipmentCategory;
                equipment.EquipmentQuantity = request.Quantity;

                var result = await _equipmentRepo.UpdateAsync(equipment);
                if (result)
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(userId, "EQUIPMENT_UPDATED", new { Name = oldName }, new { Name = equipment.EquipmentName });
                    await _notificationService.NotifyEquipmentUpdated(equipment.EquipmentName, "UPDATED");
                }
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
                var equipment = await _equipmentRepo.GetByIdAsync(equipmentId);
                var result = await _equipmentRepo.DeleteAsync(equipmentId);
                if (result && equipment != null)
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(userId, "EQUIPMENT_DELETED", new { equipment.EquipmentName }, null);
                    await _notificationService.NotifyEquipmentUpdated(equipment.EquipmentName, "DELETED");
                }
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

        // GET: Admin/Equipment/GetUnitsByStatus - For Dashboard status card modal
        [HttpGet("GetUnitsByStatus")]
        public async Task<IActionResult> GetUnitsByStatus([FromQuery] string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return Json(new { success = false, message = "Status is required" });

                var allUnits = await _unitRepo.GetAllAsync();
                var filteredUnits = allUnits.Where(u => u.EunitStatus.Equals(status, StringComparison.OrdinalIgnoreCase));

                var result = new List<object>();
                foreach (var unit in filteredUnits)
                {
                    var equipment = await _equipmentRepo.GetByIdAsync(unit.EquipmentId);
                    result.Add(new
                    {
                        SerialNo = unit.EunitSerialNo,
                        EquipmentName = equipment?.EquipmentName ?? "Unknown",
                        Category = equipment?.EquipmentCategory ?? "Unknown",
                        Condition = unit.EunitCondition,
                        Status = unit.EunitStatus,
                        LastUpdated = unit.EunitLastUpdated
                    });
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching units by status {Status}", status);
                return Json(new { success = false, message = "Failed to fetch units" });
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
                    // Auto-increment equipment_quantity when unit is added
                    await _db.ExecuteNonQueryAsync(
                        "UPDATE equipment SET equipment_quantity = equipment_quantity + 1 WHERE equipment_id = @id",
                        new[] { new NpgsqlParameter("@id", request.EquipmentId) });

                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Equipment unit created: {SerialNo} by user {UserId}", request.SerialNo, userId);
                    
                    await _auditService.LogActionAsync(userId, "UNIT_CREATED", null, new { request.SerialNo, request.EquipmentId });
                    await _notificationService.NotifyStatsUpdated();
                    
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

                var oldSerial = unit.EunitSerialNo;
                var oldCondition = unit.EunitCondition;
                var oldStatus = unit.EunitStatus;
                
                unit.EunitSerialNo = request.SerialNo ?? unit.EunitSerialNo;
                unit.EunitCondition = request.Condition;
                
                // Auto-update status based on condition (similar to sp_update_unit_condition logic)
                // If condition is Good and status is maintenance -> set to available
                // If condition is Damaged and status is available -> set to maintenance
                if (request.Condition && oldStatus == "maintenance")
                {
                    // Fixed: condition is Good and was in maintenance -> make available
                    unit.EunitStatus = "available";
                }
                else if (!request.Condition && oldStatus == "available")
                {
                    // Damaged: condition is Damaged and was available -> set to maintenance
                    unit.EunitStatus = "maintenance";
                }
                else
                {
                    unit.EunitStatus = request.Status ?? unit.EunitStatus;
                }

                var result = await _unitRepo.UpdateAsync(unit);
                if (result)
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(userId, "UNIT_UPDATED", new { SerialNo = oldSerial }, new { SerialNo = unit.EunitSerialNo });
                    await _notificationService.NotifyStatsUpdated();
                }
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

                var oldCondition = unit.EunitCondition;
                var oldStatus = unit.EunitStatus;
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                
                // Call stored procedure sp_update_unit_condition
                var query = "CALL sp_update_unit_condition(@p_unit_id, @p_condition, @p_user_id)";
                var parameters = new NpgsqlParameter[]
                {
                    new("@p_unit_id", unitId),
                    new("@p_condition", condition),
                    new("@p_user_id", userId)
                };
                
                await _db.ExecuteNonQueryAsync(query, parameters);
                
                // Reload unit to get updated status
                unit = await _unitRepo.GetByIdAsync(unitId);
                var conditionText = condition ? "Good" : "Damaged";
                var statusChanged = oldStatus != unit?.EunitStatus;
                
                _logger.LogInformation("Unit {UnitId} condition updated to {Condition}, status: {Status} by user {UserId}", 
                    unitId, conditionText, unit?.EunitStatus, userId);

                await _notificationService.NotifyEquipmentStatusChange(unit?.EunitSerialNo ?? "", unit?.EunitSerialNo ?? "", conditionText);
                await _notificationService.NotifyStatsUpdated();

                var message = statusChanged 
                    ? $"Condition updated to {conditionText}. Status changed to {unit?.EunitStatus}."
                    : $"Condition updated to {conditionText}";
                    
                return Json(new { success = true, message = message });
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

                var oldStatus = unit.EunitStatus;
                unit.EunitStatus = status.ToLower();
                var result = await _unitRepo.UpdateAsync(unit);

                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _logger.LogInformation("Unit {UnitId} status updated to {Status} by user {UserId}", unitId, status, userId);

                if (result)
                {
                    await _auditService.LogActionAsync(userId, "UNIT_STATUS_CHANGED", 
                        new { Status = oldStatus }, 
                        new { Status = status });
                    await _notificationService.NotifyEquipmentStatusChange(unit.EunitSerialNo, unit.EunitSerialNo, status);
                }

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
                var unit = await _unitRepo.GetByIdAsync(unitId);
                if (unit == null)
                    return Json(new { success = false, message = "Unit not found" });

                var equipmentId = unit.EquipmentId;
                var result = await _unitRepo.DeleteAsync(unitId);
                
                if (result)
                {
                    // Auto-decrement equipment_quantity when unit is deleted
                    await _db.ExecuteNonQueryAsync(
                        "UPDATE equipment SET equipment_quantity = GREATEST(equipment_quantity - 1, 0) WHERE equipment_id = @id",
                        new[] { new NpgsqlParameter("@id", equipmentId) });

                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(userId, "UNIT_DELETED", new { unit.EunitSerialNo }, null);
                    await _notificationService.NotifyStatsUpdated();
                }
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

        // POST: Admin/Equipment/SyncQuantities - Fix any quantity mismatches
        [HttpPost("SyncQuantities")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncQuantities()
        {
            try
            {
                // Update all equipment quantities to match actual unit counts
                var query = @"
                    UPDATE equipment e
                    SET equipment_quantity = (
                        SELECT COUNT(*) FROM equipment_unit eu WHERE eu.equipment_id = e.equipment_id
                    )";
                
                await _db.ExecuteNonQueryAsync(query);
                
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _logger.LogInformation("Equipment quantities synced by user {UserId}", userId);
                await _auditService.LogActionAsync(userId, "EQUIPMENT_QUANTITIES_SYNCED", null, null);
                await _notificationService.NotifyStatsUpdated();
                
                return Json(new { success = true, message = "Quantities synchronized with actual unit counts" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing quantities");
                return Json(new { success = false, message = ex.Message });
            }
        }

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
