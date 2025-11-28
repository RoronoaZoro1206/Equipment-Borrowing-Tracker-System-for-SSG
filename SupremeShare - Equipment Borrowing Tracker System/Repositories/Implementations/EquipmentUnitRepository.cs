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
