using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Exceptions;
using static SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces.IEquipmentUnitRepository;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations
{
    public class EquipmentUnitRepository : IEquipmentUnitRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<EquipmentUnitRepository> _logger;

        public EquipmentUnitRepository(DatabaseHelper db, IAuditService auditService, ILogger<EquipmentUnitRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<EquipmentUnit>> GetAllAsync()
        {
            string query = "SELECT * FROM equipment_unit ORDER BY eunit_id";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToEquipmentUnit(row));
        }

        public async Task<EquipmentUnit?> GetByIdAsync(int eunitId)
        {
            InputValidator.ValidateId(eunitId, "Equipment Unit ID");

            string query = "SELECT * FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToEquipmentUnit(dt.Rows[0]);
        }

        public async Task<EquipmentUnit?> GetBySerialNoAsync(string serialNo)
        {
            var sanitizedSerialNo = InputValidator.SanitizeString(serialNo, 20);
            InputValidator.ValidateSerialNumber(sanitizedSerialNo);

            string query = "SELECT * FROM equipment_unit WHERE eunit_serial_no = @serialNo";
            var parameters = new NpgsqlParameter[] { new("@serialNo", sanitizedSerialNo) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToEquipmentUnit(dt.Rows[0]);
        }

        public async Task<IEnumerable<EquipmentUnit>> GetByEquipmentIdAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "SELECT * FROM equipment_unit WHERE equipment_id = @equipmentId ORDER BY eunit_id";
            var parameters = new NpgsqlParameter[] { new("@equipmentId", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => MapToEquipmentUnit(row));
        }

        public async Task<IEnumerable<EquipmentUnit>> GetAvailableUnitsAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "SELECT * FROM equipment_unit WHERE equipment_id = @equipmentId AND eunit_status = 'available' ORDER BY eunit_id";
            var parameters = new NpgsqlParameter[] { new("@equipmentId", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => MapToEquipmentUnit(row));
        }


        public async Task<bool> AddAsync(EquipmentUnit equipmentUnit)
        {
            ValidateEquipmentUnit(equipmentUnit);

            string query = @"INSERT INTO equipment_unit 
                (equipment_id, eunit_serial_no, eunit_condition, eunit_status)
                VALUES (@equipmentId, @serialNo, @condition, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@equipmentId", equipmentUnit.EquipmentId),
                new("@serialNo", InputValidator.SanitizeString(equipmentUnit.EunitSerialNo, 20)),
                new("@condition", equipmentUnit.EunitCondition),
                new("@status", equipmentUnit.EunitStatus.ToLower())
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_UNIT_CREATED", null,
                    new { equipmentUnit.EunitSerialNo, equipmentUnit.EquipmentId, equipmentUnit.EunitStatus });
                _logger.LogInformation("Equipment unit created: {SerialNo}", equipmentUnit.EunitSerialNo);
            }

            return result;
        }

        public async Task<bool> UpdateAsync(EquipmentUnit equipmentUnit)
        {
            InputValidator.ValidateId(equipmentUnit.EunitId, "Equipment Unit ID");
            ValidateEquipmentUnit(equipmentUnit);

            var existingUnit = await GetByIdAsync(equipmentUnit.EunitId);
            if (existingUnit == null)
                throw new NotFoundException("Equipment unit not found.");

            string query = @"UPDATE equipment_unit SET 
                equipment_id = @equipmentId, eunit_serial_no = @serialNo,
                eunit_condition = @condition, eunit_status = @status,
                eunit_last_updated = CURRENT_TIMESTAMP
                WHERE eunit_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", equipmentUnit.EunitId),
                new("@equipmentId", equipmentUnit.EquipmentId),
                new("@serialNo", InputValidator.SanitizeString(equipmentUnit.EunitSerialNo, 20)),
                new("@condition", equipmentUnit.EunitCondition),
                new("@status", equipmentUnit.EunitStatus.ToLower())
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_UNIT_UPDATED",
                    new { existingUnit.EunitSerialNo, existingUnit.EunitStatus },
                    new { equipmentUnit.EunitSerialNo, equipmentUnit.EunitStatus });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int eunitId)
        {
            InputValidator.ValidateId(eunitId, "Equipment Unit ID");

            var existingUnit = await GetByIdAsync(eunitId);
            if (existingUnit == null)
                throw new NotFoundException("Equipment unit not found.");

            // Prevent deletion of borrowed units
            if (existingUnit.EunitStatus.ToLower() == "borrowed")
                throw new ValidationException("Cannot delete a borrowed equipment unit.");

            string query = "DELETE FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_UNIT_DELETED",
                    new { existingUnit.EunitSerialNo }, null);
                _logger.LogWarning("Equipment unit deleted: {EunitId}", eunitId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int eunitId)
        {
            InputValidator.ValidateId(eunitId, "Equipment Unit ID");

            string query = "SELECT COUNT(1) FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Gets all equipment units with status 'for review' (returned damaged, pending admin decision)
        /// </summary>
        public async Task<IEnumerable<EquipmentUnitForReview>> GetUnitsForReviewAsync()
        {
            string query = @"
                SELECT 
                    eu.eunit_id,
                    eu.equipment_id,
                    eu.eunit_serial_no,
                    e.equipment_name,
                    e.equipment_category,
                    eu.eunit_last_updated,
                    br.borrow_date_returned,
                    s.student_first_name,
                    s.student_last_name,
                    s.student_id_number
                FROM equipment_unit eu
                JOIN equipment e ON eu.equipment_id = e.equipment_id
                LEFT JOIN borrow_record br ON br.eunit_id = eu.eunit_id 
                    AND br.borrow_status = 'returned'
                LEFT JOIN transaction t ON br.transaction_id = t.transaction_id
                LEFT JOIN student s ON t.student_id = s.student_id
                WHERE eu.eunit_status = 'for review'
                ORDER BY eu.eunit_last_updated DESC";

            var dt = await _db.ExecuteQueryAsync(query);
            
            return dt.AsEnumerable().Select(row => new EquipmentUnitForReview
            {
                EunitId = Convert.ToInt32(row["eunit_id"]),
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EunitSerialNo = row["eunit_serial_no"]?.ToString() ?? "",
                EquipmentName = row["equipment_name"]?.ToString() ?? "",
                EquipmentCategory = row["equipment_category"]?.ToString() ?? "",
                EunitLastUpdated = Convert.ToDateTime(row["eunit_last_updated"]),
                ReturnedDate = row["borrow_date_returned"] != DBNull.Value 
                    ? Convert.ToDateTime(row["borrow_date_returned"]) 
                    : DateTime.MinValue,
                ReturnedByStudentName = row["student_first_name"] != DBNull.Value && row["student_last_name"] != DBNull.Value
                    ? $"{row["student_first_name"]} {row["student_last_name"]}"
                    : null,
                ReturnedByStudentIdNumber = row["student_id_number"]?.ToString()
            });
        }

        /// <summary>
        /// Admin processes a unit review using sp_update_unit_condition.
        /// TRUE = Good (moves to 'available'), FALSE = Damaged (moves to 'maintenance')
        /// </summary>
        public async Task<ReviewProcessResult> ProcessUnitReviewAsync(int unitId, bool condition, int userId)
        {
            InputValidator.ValidateId(unitId, "Unit ID");
            InputValidator.ValidateId(userId, "User ID");

            // Verify unit exists and is in 'for review' status
            var unit = await GetByIdAsync(unitId);
            if (unit == null)
                throw new NotFoundException("Equipment unit not found.");

            if (unit.EunitStatus.ToLower() != "for review")
                throw new ValidationException($"Unit is not pending review. Current status: {unit.EunitStatus}");

            string query = "CALL sp_update_unit_condition(@p_unit_id, @p_condition, @p_user_id)";
            var parameters = new NpgsqlParameter[]
            {
                new("@p_unit_id", unitId),
                new("@p_condition", condition),
                new("@p_user_id", userId)
            };

            await _db.ExecuteNonQueryAsync(query, parameters);

            var newStatus = condition ? "available" : "maintenance";
            var result = new ReviewProcessResult
            {
                Success = true,
                NewStatus = newStatus,
                Message = condition 
                    ? "Unit marked as Good and is now available for borrowing." 
                    : "Unit confirmed as Damaged and sent to maintenance."
            };

            await _auditService.LogActionAsync(userId, "UNIT_REVIEW_PROCESSED",
                new { UnitId = unitId, SerialNo = unit.EunitSerialNo, OldStatus = "for review" },
                new { NewStatus = newStatus, Condition = condition ? "Good" : "Damaged" });

            _logger.LogInformation(
                "Unit review processed: UnitId {UnitId}, SerialNo {SerialNo}, Decision {Decision}, NewStatus {NewStatus}",
                unitId, unit.EunitSerialNo, condition ? "Good" : "Damaged", newStatus);

            return result;
        }

        private void ValidateEquipmentUnit(EquipmentUnit unit)
        {
            InputValidator.ValidateId(unit.EquipmentId, "Equipment ID");
            InputValidator.ValidateSerialNumber(unit.EunitSerialNo);
            InputValidator.ValidateEquipmentStatus(unit.EunitStatus);
        }

        private EquipmentUnit MapToEquipmentUnit(DataRow row)
        {
            return new EquipmentUnit
            {
                EunitId = Convert.ToInt32(row["eunit_id"]),
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EunitSerialNo = row["eunit_serial_no"].ToString() ?? "",
                EunitCondition = Convert.ToBoolean(row["eunit_condition"]),
                EunitStatus = row["eunit_status"].ToString() ?? "",
                EunitDateAdded = Convert.ToDateTime(row["eunit_date_added"]),
                EunitLastUpdated = Convert.ToDateTime(row["eunit_last_updated"])
            };
        }
    }
}
