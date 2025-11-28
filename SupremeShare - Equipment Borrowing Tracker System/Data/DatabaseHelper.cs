using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseHelper> _logger;

        public DatabaseHelper(IConfiguration configuration, ILogger<DatabaseHelper> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
            _logger = logger;
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new NpgsqlCommand(query, connection);
                command.CommandTimeout = 30; // Prevent long-running queries
                
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                var dataTable = new DataTable();
                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database query error");
                throw new DatabaseException("An error occurred while executing the query.", ex);
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new NpgsqlCommand(query, connection);
                command.CommandTimeout = 30;
                
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database non-query error");
                throw new DatabaseException("An error occurred while executing the command.", ex);
            }
        }


        public async Task<object?> ExecuteScalarAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new NpgsqlCommand(query, connection);
                command.CommandTimeout = 30;
                
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                return await command.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database scalar query error");
                throw new DatabaseException("An error occurred while executing the query.", ex);
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> operation)
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                var result = await operation(connection, transaction);
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction error - rolled back");
                throw new DatabaseException("Transaction failed and was rolled back.", ex);
            }
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
