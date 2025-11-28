using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly DatabaseHelper _db;

        public EquipmentRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Equipment>> GetAllAsync()
        {
            string query = "SELECT * FROM equipment ORDER BY equipment_id";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Equipment
            {
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EquipmentName = row["equipment_name"].ToString() ?? "",
                EquipmentCategory = row["equipment_category"].ToString() ?? "",
                EquipmentQuantity = Convert.ToInt32(row["equipment_quantity"]),
                EquipmentDateAdded = Convert.ToDateTime(row["equipment_date_added"]),
                EquipmentLastUpdated = Convert.ToDateTime(row["equipment_last_updated"])
            });
        }

        public async Task<Equipment?> GetByIdAsync(int equipmentId)
        {
            string query = "SELECT * FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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


        public async Task<bool> AddAsync(Equipment equipment)
        {
            string query = @"INSERT INTO equipment 
                (equipment_name, equipment_category, equipment_quantity)
                VALUES (@name, @category, @quantity)";

            var parameters = new NpgsqlParameter[]
            {
                new("@name", equipment.EquipmentName),
                new("@category", equipment.EquipmentCategory),
                new("@quantity", equipment.EquipmentQuantity)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Equipment equipment)
        {
            string query = @"UPDATE equipment SET 
                equipment_name = @name, equipment_category = @category,
                equipment_quantity = @quantity, equipment_last_updated = CURRENT_TIMESTAMP
                WHERE equipment_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", equipment.EquipmentId),
                new("@name", equipment.EquipmentName),
                new("@category", equipment.EquipmentCategory),
                new("@quantity", equipment.EquipmentQuantity)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int equipmentId)
        {
            string query = "DELETE FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int equipmentId)
        {
            string query = "SELECT COUNT(1) FROM equipment WHERE equipment_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<int> GetAvailableStockAsync(int equipmentId)
        {
            string query = "SELECT fn_get_available_stock(@id)";
            var parameters = new NpgsqlParameter[] { new("@id", equipmentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
    }
}
