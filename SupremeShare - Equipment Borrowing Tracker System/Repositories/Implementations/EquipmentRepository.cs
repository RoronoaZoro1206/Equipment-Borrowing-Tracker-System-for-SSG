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
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<EquipmentRepository> _logger;

        public EquipmentRepository(DatabaseHelper db, IAuditService auditService, ILogger<EquipmentRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<Equipment>> GetAllAsync()
        {
            string query = "SELECT * FROM equipment ORDER BY equipment_id";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToEquipment(row));
        }

        public async Task<Equipment?> GetByIdAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "SELECT * FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToEquipment(dt.Rows[0]);
        }

        public async Task<bool> AddAsync(Equipment equipment)
        {
            ValidateEquipment(equipment);

            string query = @"INSERT INTO equipment 
                (equipment_name, equipment_category, equipment_quantity)
                VALUES (@name, @category, @quantity)";

            var parameters = new NpgsqlParameter[]
            {
                new("@name", InputValidator.SanitizeString(equipment.EquipmentName, 100)),
                new("@category", InputValidator.SanitizeString(equipment.EquipmentCategory, 50)),
                new("@quantity", equipment.EquipmentQuantity)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_CREATED", null,
                    new { equipment.EquipmentName, equipment.EquipmentCategory, equipment.EquipmentQuantity });
                _logger.LogInformation("Equipment created: {EquipmentName}", equipment.EquipmentName);
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Equipment equipment)
        {
            InputValidator.ValidateId(equipment.EquipmentId, "Equipment ID");
            ValidateEquipment(equipment);

            var existingEquipment = await GetByIdAsync(equipment.EquipmentId);
            if (existingEquipment == null)
                throw new NotFoundException("Equipment not found.");

            string query = @"UPDATE equipment SET 
                equipment_name = @name, equipment_category = @category,
                equipment_quantity = @quantity, equipment_last_updated = CURRENT_TIMESTAMP
                WHERE equipment_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", equipment.EquipmentId),
                new("@name", InputValidator.SanitizeString(equipment.EquipmentName, 100)),
                new("@category", InputValidator.SanitizeString(equipment.EquipmentCategory, 50)),
                new("@quantity", equipment.EquipmentQuantity)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_UPDATED",
                    new { existingEquipment.EquipmentName, existingEquipment.EquipmentQuantity },
                    new { equipment.EquipmentName, equipment.EquipmentQuantity });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            var existingEquipment = await GetByIdAsync(equipmentId);
            if (existingEquipment == null)
                throw new NotFoundException("Equipment not found.");

            // Check if equipment has active units before deletion
            string checkQuery = "SELECT COUNT(1) FROM equipment_unit WHERE equipment_id = @id AND eunit_status != 'available'";
            var checkParams = new NpgsqlParameter[] { new("@id", equipmentId) };
            var activeUnits = Convert.ToInt32(await _db.ExecuteScalarAsync(checkQuery, checkParams));
            
            if (activeUnits > 0)
                throw new ValidationException("Cannot delete equipment with active borrowed units.");

            string query = "DELETE FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "EQUIPMENT_DELETED",
                    new { existingEquipment.EquipmentName }, null);
                _logger.LogWarning("Equipment deleted: {EquipmentId}", equipmentId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "SELECT COUNT(1) FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<int> GetAvailableStockAsync(int equipmentId)
        {
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "SELECT fn_get_available_stock(@id)";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }

        private void ValidateEquipment(Equipment equipment)
        {
            if (string.IsNullOrWhiteSpace(equipment.EquipmentName))
                throw new ValidationException("Equipment name is required.");
            if (string.IsNullOrWhiteSpace(equipment.EquipmentCategory))
                throw new ValidationException("Equipment category is required.");
            if (equipment.EquipmentQuantity < 0)
                throw new ValidationException("Equipment quantity cannot be negative.");
        }

        private Equipment MapToEquipment(DataRow row)
        {
            return new Equipment
            {
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EquipmentName = row["equipment_name"].ToString() ?? "",
                EquipmentCategory = row["equipment_category"].ToString() ?? "",
                EquipmentQuantity = Convert.ToInt32(row["equipment_quantity"]),
                EquipmentDateAdded = Convert.ToDateTime(row["equipment_date_added"]),
                EquipmentLastUpdated = Convert.ToDateTime(row["equipment_last_updated"])
            };
        }
    }
}
