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
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly DatabaseHelper _db;

        public AuditLogRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            string query = "SELECT * FROM audit_log ORDER BY audit_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new AuditLog
            {
                AuditId = Convert.ToInt32(row["audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuditAction = row["audit_action"].ToString() ?? "",
                AuditOldData = row["audit_old_data"]?.ToString(),
                AuditNewData = row["audit_new_data"]?.ToString(),
                AuditLoggedAt = Convert.ToDateTime(row["audit_logged_at"])
            });
        }

        public async Task<AuditLog?> GetByIdAsync(int auditId)
        {
            string query = "SELECT * FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new AuditLog
            {
                AuditId = Convert.ToInt32(row["audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuditAction = row["audit_action"].ToString() ?? "",
                AuditOldData = row["audit_old_data"]?.ToString(),
                AuditNewData = row["audit_new_data"]?.ToString(),
                AuditLoggedAt = Convert.ToDateTime(row["audit_logged_at"])
            };
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            string query = "SELECT * FROM audit_log WHERE user_id = @userId ORDER BY audit_id DESC";
            var parameters = new NpgsqlParameter[] { new("@userId", userId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new AuditLog
            {
                AuditId = Convert.ToInt32(row["audit_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                AuditAction = row["audit_action"].ToString() ?? "",
                AuditOldData = row["audit_old_data"]?.ToString(),
                AuditNewData = row["audit_new_data"]?.ToString(),
                AuditLoggedAt = Convert.ToDateTime(row["audit_logged_at"])
            });
        }

        public async Task<bool> AddAsync(AuditLog auditLog)
        {
            string query = @"INSERT INTO audit_log 
                (user_id, audit_action, audit_old_data, audit_new_data)
                VALUES (@userId, @action, @oldData, @newData)";

            var parameters = new NpgsqlParameter[]
            {
                new("@userId", auditLog.UserId),
                new("@action", auditLog.AuditAction),
                new("@oldData", (object?)auditLog.AuditOldData ?? DBNull.Value),
                new("@newData", (object?)auditLog.AuditNewData ?? DBNull.Value)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int auditId)
        {
            string query = "DELETE FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int auditId)
        {
            string query = "SELECT COUNT(1) FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
