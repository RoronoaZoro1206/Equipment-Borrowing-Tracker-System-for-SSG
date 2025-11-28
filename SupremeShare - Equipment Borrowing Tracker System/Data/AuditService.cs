using System;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public interface IAuditService
    {
        Task LogActionAsync(int userId, string action, object? oldData = null, object? newData = null);
    }

    public class AuditService : IAuditService
    {
        private readonly DatabaseHelper _db;
        private readonly ILogger<AuditService> _logger;

        public AuditService(DatabaseHelper db, ILogger<AuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task LogActionAsync(int userId, string action, object? oldData = null, object? newData = null)
        {
            try
            {
                string query = @"INSERT INTO audit_log 
                    (user_id, audit_action, audit_old_data, audit_new_data)
                    VALUES (@userId, @action, @oldData, @newData)";

                var oldDataJson = oldData != null ? JsonSerializer.Serialize(oldData) : null;
                var newDataJson = newData != null ? JsonSerializer.Serialize(newData) : null;

                var parameters = new NpgsqlParameter[]
                {
                    new("@userId", userId),
                    new("@action", action),
                    new("@oldData", (object?)oldDataJson ?? DBNull.Value),
                    new("@newData", (object?)newDataJson ?? DBNull.Value)
                };

                await _db.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                // Log but don't throw - audit failures shouldn't break main operations
                _logger.LogError(ex, "Failed to log audit action: {Action}", action);
            }
        }
    }
}
