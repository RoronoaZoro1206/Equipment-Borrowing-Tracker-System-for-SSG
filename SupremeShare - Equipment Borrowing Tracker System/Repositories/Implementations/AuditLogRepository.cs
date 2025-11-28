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
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<AuditLogRepository> _logger;

        public AuditLogRepository(DatabaseHelper db, ILogger<AuditLogRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            // Limit results for performance and security
            string query = "SELECT * FROM audit_log ORDER BY audit_id DESC LIMIT 1000";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToAuditLog(row));
        }

        public async Task<AuditLog?> GetByIdAsync(int auditId)
        {
            InputValidator.ValidateId(auditId, "Audit ID");

            string query = "SELECT * FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToAuditLog(dt.Rows[0]);
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            InputValidator.ValidateId(userId, "User ID");

            string query = "SELECT * FROM audit_log WHERE user_id = @userId ORDER BY audit_id DESC LIMIT 500";
            var parameters = new NpgsqlParameter[] { new("@userId", userId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => MapToAuditLog(row));
        }

        public async Task<bool> AddAsync(AuditLog auditLog)
        {
            InputValidator.ValidateId(auditLog.UserId, "User ID");
            if (string.IsNullOrWhiteSpace(auditLog.AuditAction))
                throw new ValidationException("Audit action is required.");

            string query = @"INSERT INTO audit_log 
                (user_id, audit_action, audit_old_data, audit_new_data)
                VALUES (@userId, @action, @oldData, @newData)";

            var parameters = new NpgsqlParameter[]
            {
                new("@userId", auditLog.UserId),
                new("@action", InputValidator.SanitizeString(auditLog.AuditAction, 255)),
                new("@oldData", (object?)auditLog.AuditOldData ?? DBNull.Value),
                new("@newData", (object?)auditLog.AuditNewData ?? DBNull.Value)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int auditId)
        {
            // Audit logs should generally not be deleted - this is for admin use only
            InputValidator.ValidateId(auditId, "Audit ID");

            _logger.LogWarning("Audit log deletion attempted: {AuditId}", auditId);

            string query = "DELETE FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int auditId)
        {
            InputValidator.ValidateId(auditId, "Audit ID");

            string query = "SELECT COUNT(1) FROM audit_log WHERE audit_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", auditId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        private AuditLog MapToAuditLog(DataRow row)
        {
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
    }
}
