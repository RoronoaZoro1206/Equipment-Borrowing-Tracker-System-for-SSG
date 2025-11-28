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
    public class EquipmentUnitRepository : IEquipmentUnitRepository
    {
        private readonly DatabaseHelper _db;

        public EquipmentUnitRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<EquipmentUnit>> GetAllAsync()
        {
            string query = "SELECT * FROM equipment_unit ORDER BY eunit_id";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new EquipmentUnit
            {
                EunitId = Convert.ToInt32(row["eunit_id"]),
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EunitSerialNo = row["eunit_serial_no"].ToString() ?? "",
                EunitCondition = Convert.ToBoolean(row["eunit_condition"]),
                EunitStatus = row["eunit_status"].ToString() ?? "",
                EunitDateAdded = Convert.ToDateTime(row["eunit_date_added"]),
                EunitLastUpdated = Convert.ToDateTime(row["eunit_last_updated"])
            });
        }

        public async Task<EquipmentUnit?> GetByIdAsync(int eunitId)
        {
            string query = "SELECT * FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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


        public async Task<EquipmentUnit?> GetBySerialNoAsync(string serialNo)
        {
            string query = "SELECT * FROM equipment_unit WHERE eunit_serial_no = @serialNo";
            var parameters = new NpgsqlParameter[] { new("@serialNo", serialNo) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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

        public async Task<IEnumerable<EquipmentUnit>> GetByEquipmentIdAsync(int equipmentId)
        {
            string query = "SELECT * FROM equipment_unit WHERE equipment_id = @equipmentId ORDER BY eunit_id";
            var parameters = new NpgsqlParameter[] { new("@equipmentId", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new EquipmentUnit
            {
                EunitId = Convert.ToInt32(row["eunit_id"]),
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EunitSerialNo = row["eunit_serial_no"].ToString() ?? "",
                EunitCondition = Convert.ToBoolean(row["eunit_condition"]),
                EunitStatus = row["eunit_status"].ToString() ?? "",
                EunitDateAdded = Convert.ToDateTime(row["eunit_date_added"]),
                EunitLastUpdated = Convert.ToDateTime(row["eunit_last_updated"])
            });
        }

        public async Task<IEnumerable<EquipmentUnit>> GetAvailableUnitsAsync(int equipmentId)
        {
            string query = "SELECT * FROM equipment_unit WHERE equipment_id = @equipmentId AND eunit_status = 'available' ORDER BY eunit_id";
            var parameters = new NpgsqlParameter[] { new("@equipmentId", equipmentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new EquipmentUnit
            {
                EunitId = Convert.ToInt32(row["eunit_id"]),
                EquipmentId = Convert.ToInt32(row["equipment_id"]),
                EunitSerialNo = row["eunit_serial_no"].ToString() ?? "",
                EunitCondition = Convert.ToBoolean(row["eunit_condition"]),
                EunitStatus = row["eunit_status"].ToString() ?? "",
                EunitDateAdded = Convert.ToDateTime(row["eunit_date_added"]),
                EunitLastUpdated = Convert.ToDateTime(row["eunit_last_updated"])
            });
        }

        public async Task<bool> AddAsync(EquipmentUnit equipmentUnit)
        {
            string query = @"INSERT INTO equipment_unit 
                (equipment_id, eunit_serial_no, eunit_condition, eunit_status)
                VALUES (@equipmentId, @serialNo, @condition, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@equipmentId", equipmentUnit.EquipmentId),
                new("@serialNo", equipmentUnit.EunitSerialNo),
                new("@condition", equipmentUnit.EunitCondition),
                new("@status", equipmentUnit.EunitStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(EquipmentUnit equipmentUnit)
        {
            string query = @"UPDATE equipment_unit SET 
                equipment_id = @equipmentId, eunit_serial_no = @serialNo,
                eunit_condition = @condition, eunit_status = @status,
                eunit_last_updated = CURRENT_TIMESTAMP
                WHERE eunit_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", equipmentUnit.EunitId),
                new("@equipmentId", equipmentUnit.EquipmentId),
                new("@serialNo", equipmentUnit.EunitSerialNo),
                new("@condition", equipmentUnit.EunitCondition),
                new("@status", equipmentUnit.EunitStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int eunitId)
        {
            string query = "DELETE FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int eunitId)
        {
            string query = "SELECT COUNT(1) FROM equipment_unit WHERE eunit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", eunitId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
