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
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Check your .env file.");
            _logger = logger;
            
            _logger.LogInformation("DatabaseHelper initialized");
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                _logger.LogInformation("Database connection successful!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed!");
                return false;
            }
        }

        /// <summary>
        /// Gets an open database connection
        /// </summary>
        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Executes a SELECT query and returns results as DataTable
        /// </summary>
        public async Task<DataTable> ExecuteQueryAsync(string query, NpgsqlParameter[]? parameters = null)
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

                var dataTable = new DataTable();
                using var reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
                return dataTable;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database query error: {Query}", TruncateQuery(query));
                throw new DatabaseException("An error occurred while executing the query.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during query execution");
                throw new DatabaseException("An unexpected error occurred.", ex);
            }
        }

        /// <summary>
        /// Executes INSERT, UPDATE, DELETE queries
        /// </summary>
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
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database non-query error: {Query}", TruncateQuery(query));
                throw new DatabaseException("An error occurred while executing the command.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during command execution");
                throw new DatabaseException("An unexpected error occurred.", ex);
            }
        }

        /// <summary>
        /// Executes a query that returns a single value
        /// </summary>
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
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database scalar query error");
                throw new DatabaseException("An error occurred while executing the query.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during scalar query");
                throw new DatabaseException("An unexpected error occurred.", ex);
            }
        }

        /// <summary>
        /// Executes multiple operations in a transaction
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> operation)
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                var result = await operation(connection, transaction);
                await transaction.CommitAsync();
                _logger.LogDebug("Transaction committed successfully");
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Transaction rolled back due to error");
                throw new DatabaseException("Transaction failed and was rolled back.", ex);
            }
        }

        /// <summary>
        /// Truncates query for logging (security - don't log full queries with data)
        /// </summary>
        private string TruncateQuery(string query)
        {
            const int maxLength = 100;
            if (query.Length <= maxLength)
                return query;
            return query.Substring(0, maxLength) + "...";
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
