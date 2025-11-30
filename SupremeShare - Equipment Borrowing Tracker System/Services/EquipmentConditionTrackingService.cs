using System.Data;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Services
{
    /// <summary>
    /// Service for tracking equipment condition changes using the existing AUDIT_LOG table.
    /// No additional tables required - leverages existing schema.
    /// </summary>
    public interface IEquipmentConditionTrackingService
    {
        Task LogConditionChangeAsync(int unitId, bool oldCondition, bool newCondition, int userId, string? remarks = null);
        Task<List<EquipmentConditionHistory>> GetConditionHistoryAsync(int unitId);
        Task<List<EquipmentConditionHistory>> GetAllConditionChangesAsync(int limit = 100);
        Task<List<EquipmentConditionSummary>> GetConditionSummaryAsync();
        Task<bool> UpdateUnitConditionAsync(int unitId, bool newCondition, int userId, string? remarks = null);
    }

    public class EquipmentConditionTrackingService : IEquipmentConditionTrackingService
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<EquipmentConditionTrackingService> _logger;

        public EquipmentConditionTrackingService(
            DatabaseHelper db, 
            IAuditService auditService,
            ILogger<EquipmentConditionTrackingService> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task LogConditionChangeAsync(int unitId, bool oldCondition, bool newCondition, int userId, string? remarks = null)
        {
            try
            {
                // Get unit details for logging
                var unitQuery = @"
                    SELECT eu.eunit_serial_no, e.equipment_name 
                    FROM equipment_unit eu 
                    JOIN equipment e ON eu.equipment_id = e.equipment_id 
                    WHERE eu.eunit_id = @unit_id";
                var unitParams = new NpgsqlParameter[] { new("@unit_id", unitId) };
                var unitDt = await _db.ExecuteQueryAsync(unitQuery, unitParams);

                var serialNo = unitDt.Rows.Count > 0 ? unitDt.Rows[0]["eunit_serial_no"]?.ToString() : "Unknown";
                var equipmentName = unitDt.Rows.Count > 0 ? unitDt.Rows[0]["equipment_name"]?.ToString() : "Unknown";

                // Log to AUDIT_LOG using existing audit service
                var oldData = $"Condition: {(oldCondition ? "Good" : "Damaged")} | Unit: {serialNo} | Equipment: {equipmentName}";
                var newData = $"Condition: {(newCondition ? "Good" : "Damaged")}{(string.IsNullOrEmpty(remarks) ? "" : $" | Remarks: {remarks}")}";

                await _auditService.LogActionAsync(userId, $"CONDITION_CHANGE: Unit {serialNo}", oldData, newData);

                _logger.LogInformation("Condition change logged for unit {UnitId} ({SerialNo}): {Old} -> {New}", 
                    unitId, serialNo, oldCondition ? "Good" : "Damaged", newCondition ? "Good" : "Damaged");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging condition change for unit {UnitId}", unitId);
                throw;
            }
        }

        public async Task<bool> UpdateUnitConditionAsync(int unitId, bool newCondition, int userId, string? remarks = null)
        {
            try
            {
                // Get current condition
                var currentQuery = "SELECT eunit_condition FROM equipment_unit WHERE eunit_id = @unit_id";
                var currentParams = new NpgsqlParameter[] { new("@unit_id", unitId) };
                var currentDt = await _db.ExecuteQueryAsync(currentQuery, currentParams);

                if (currentDt.Rows.Count == 0)
                {
                    _logger.LogWarning("Unit {UnitId} not found", unitId);
                    return false;
                }

                var oldCondition = Convert.ToBoolean(currentDt.Rows[0]["eunit_condition"]);

                if (oldCondition == newCondition)
                {
                    _logger.LogInformation("No condition change needed for unit {UnitId}", unitId);
                    return true;
                }

                // Update the condition
                var updateQuery = @"
                    UPDATE equipment_unit 
                    SET eunit_condition = @new_condition, eunit_last_updated = NOW()
                    WHERE eunit_id = @unit_id";
                var updateParams = new NpgsqlParameter[]
                {
                    new("@unit_id", unitId),
                    new("@new_condition", newCondition)
                };

                await _db.ExecuteNonQueryAsync(updateQuery, updateParams);

                // Log the change
                await LogConditionChangeAsync(unitId, oldCondition, newCondition, userId, remarks);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating condition for unit {UnitId}", unitId);
                throw;
            }
        }

        public async Task<List<EquipmentConditionHistory>> GetConditionHistoryAsync(int unitId)
        {
            try
            {
                // Get unit serial number first
                var unitQuery = "SELECT eunit_serial_no FROM equipment_unit WHERE eunit_id = @unit_id";
                var unitParams = new NpgsqlParameter[] { new("@unit_id", unitId) };
                var unitDt = await _db.ExecuteQueryAsync(unitQuery, unitParams);

                if (unitDt.Rows.Count == 0)
                    return new List<EquipmentConditionHistory>();

                var serialNo = unitDt.Rows[0]["eunit_serial_no"]?.ToString();

                // Query audit log for condition changes related to this unit
                var query = @"
                    SELECT a.audit_id, a.audit_action, a.audit_old_data, a.audit_new_data, 
                           a.audit_logged_at, u.user_username, u.user_first_name, u.user_last_name
                    FROM audit_log a
                    LEFT JOIN ""USER"" u ON a.user_id = u.user_id
                    WHERE a.audit_action LIKE '%CONDITION_CHANGE%' 
                      AND a.audit_old_data LIKE @serial_pattern
                    ORDER BY a.audit_logged_at DESC
                    LIMIT 50";

                var parameters = new NpgsqlParameter[] { new("@serial_pattern", $"%{serialNo}%") };
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                return dt.AsEnumerable().Select(row => ParseConditionHistory(row, unitId, serialNo)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting condition history for unit {UnitId}", unitId);
                throw;
            }
        }

        public async Task<List<EquipmentConditionHistory>> GetAllConditionChangesAsync(int limit = 100)
        {
            try
            {
                var query = @"
                    SELECT a.audit_id, a.audit_action, a.audit_old_data, a.audit_new_data, 
                           a.audit_logged_at, u.user_username, u.user_first_name, u.user_last_name
                    FROM audit_log a
                    LEFT JOIN ""USER"" u ON a.user_id = u.user_id
                    WHERE a.audit_action LIKE '%CONDITION_CHANGE%'
                    ORDER BY a.audit_logged_at DESC
                    LIMIT @limit";

                var parameters = new NpgsqlParameter[] { new("@limit", limit) };
                var dt = await _db.ExecuteQueryAsync(query, parameters);

                return dt.AsEnumerable().Select(row => ParseConditionHistory(row, 0, null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all condition changes");
                throw;
            }
        }

        public async Task<List<EquipmentConditionSummary>> GetConditionSummaryAsync()
        {
            try
            {
                var query = @"
                    SELECT e.equipment_id, e.equipment_name, e.equipment_category,
                           COUNT(eu.eunit_id) as total_units,
                           COUNT(CASE WHEN eu.eunit_condition = true THEN 1 END) as good_units,
                           COUNT(CASE WHEN eu.eunit_condition = false THEN 1 END) as damaged_units,
                           COUNT(CASE WHEN eu.eunit_status = 'available' THEN 1 END) as available_units,
                           COUNT(CASE WHEN eu.eunit_status = 'borrowed' THEN 1 END) as borrowed_units,
                           COUNT(CASE WHEN eu.eunit_status = 'lost' THEN 1 END) as lost_units,
                           MAX(eu.eunit_last_updated) as last_updated
                    FROM equipment e
                    LEFT JOIN equipment_unit eu ON e.equipment_id = eu.equipment_id
                    GROUP BY e.equipment_id, e.equipment_name, e.equipment_category
                    ORDER BY e.equipment_name";

                var dt = await _db.ExecuteQueryAsync(query);

                return dt.AsEnumerable().Select(row => new EquipmentConditionSummary
                {
                    EquipmentId = Convert.ToInt32(row["equipment_id"]),
                    EquipmentName = row["equipment_name"]?.ToString(),
                    Category = row["equipment_category"]?.ToString(),
                    TotalUnits = Convert.ToInt32(row["total_units"]),
                    GoodUnits = Convert.ToInt32(row["good_units"]),
                    DamagedUnits = Convert.ToInt32(row["damaged_units"]),
                    AvailableUnits = Convert.ToInt32(row["available_units"]),
                    BorrowedUnits = Convert.ToInt32(row["borrowed_units"]),
                    LostUnits = Convert.ToInt32(row["lost_units"]),
                    LastConditionChange = row["last_updated"] != DBNull.Value 
                        ? Convert.ToDateTime(row["last_updated"]) : null
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting condition summary");
                throw;
            }
        }

        private static EquipmentConditionHistory ParseConditionHistory(DataRow row, int unitId, string? serialNo)
        {
            var oldData = row["audit_old_data"]?.ToString() ?? "";
            var newData = row["audit_new_data"]?.ToString() ?? "";

            // Parse condition from audit data
            var oldCondition = oldData.Contains("Good");
            var newCondition = newData.Contains("Good");

            // Extract serial number from old data if not provided
            if (string.IsNullOrEmpty(serialNo) && oldData.Contains("Unit:"))
            {
                var startIdx = oldData.IndexOf("Unit:") + 5;
                var endIdx = oldData.IndexOf("|", startIdx);
                if (endIdx > startIdx)
                    serialNo = oldData.Substring(startIdx, endIdx - startIdx).Trim();
            }

            // Extract equipment name
            string? equipmentName = null;
            if (oldData.Contains("Equipment:"))
            {
                var startIdx = oldData.IndexOf("Equipment:") + 10;
                equipmentName = oldData.Substring(startIdx).Trim();
            }

            // Extract remarks from new data
            string? remarks = null;
            if (newData.Contains("Remarks:"))
            {
                var startIdx = newData.IndexOf("Remarks:") + 8;
                remarks = newData.Substring(startIdx).Trim();
            }

            return new EquipmentConditionHistory
            {
                HistoryId = Convert.ToInt32(row["audit_id"]),
                UnitId = unitId,
                OldCondition = oldCondition,
                NewCondition = newCondition,
                ChangeDate = Convert.ToDateTime(row["audit_logged_at"]),
                Remarks = remarks,
                ChangedByUsername = row["user_username"]?.ToString(),
                ChangedByFullName = $"{row["user_first_name"]} {row["user_last_name"]}".Trim(),
                SerialNo = serialNo,
                EquipmentName = equipmentName
            };
        }
    }

    public class EquipmentConditionHistory
    {
        public int HistoryId { get; set; }
        public int UnitId { get; set; }
        public bool OldCondition { get; set; }
        public bool NewCondition { get; set; }
        public DateTime ChangeDate { get; set; }
        public string? Remarks { get; set; }
        public string? ChangedByUsername { get; set; }
        public string? ChangedByFullName { get; set; }
        public string? SerialNo { get; set; }
        public string? EquipmentName { get; set; }
    }

    public class EquipmentConditionSummary
    {
        public int EquipmentId { get; set; }
        public string? EquipmentName { get; set; }
        public string? Category { get; set; }
        public int TotalUnits { get; set; }
        public int GoodUnits { get; set; }
        public int DamagedUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int BorrowedUnits { get; set; }
        public int LostUnits { get; set; }
        public DateTime? LastConditionChange { get; set; }
    }
}
