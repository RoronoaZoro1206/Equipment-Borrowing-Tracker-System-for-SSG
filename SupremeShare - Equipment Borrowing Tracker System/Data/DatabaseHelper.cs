using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<NpgsqlConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            using var connection = await GetConnectionAsync();
            using var command = new NpgsqlCommand(query, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var dataTable = new DataTable();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);
            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            using var connection = await GetConnectionAsync();
            using var command = new NpgsqlCommand(query, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteNonQueryAsync();
        }


        public async Task<object?> ExecuteScalarAsync(string query, NpgsqlParameter[]? parameters = null)
        {
            using var connection = await GetConnectionAsync();
            using var command = new NpgsqlCommand(query, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteScalarAsync();
        }
    }
}
