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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly DatabaseHelper _db;

        public TransactionRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            string query = "SELECT * FROM transaction ORDER BY transaction_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Transaction
            {
                TransactionId = Convert.ToInt32(row["transaction_id"]),
                StudentId = Convert.ToInt32(row["student_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                TransactionCode = row["transaction_code"]?.ToString(),
                TransactionDateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]),
                TransactionRemarks = row["transaction_remarks"]?.ToString(),
                TransactionDateAdded = Convert.ToDateTime(row["transaction_date_added"]),
                TransactionLastUpdated = Convert.ToDateTime(row["transaction_last_updated"])
            });
        }

        public async Task<Transaction?> GetByIdAsync(int transactionId)
        {
            string query = "SELECT * FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Transaction
            {
                TransactionId = Convert.ToInt32(row["transaction_id"]),
                StudentId = Convert.ToInt32(row["student_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                TransactionCode = row["transaction_code"]?.ToString(),
                TransactionDateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]),
                TransactionRemarks = row["transaction_remarks"]?.ToString(),
                TransactionDateAdded = Convert.ToDateTime(row["transaction_date_added"]),
                TransactionLastUpdated = Convert.ToDateTime(row["transaction_last_updated"])
            };
        }


        public async Task<Transaction?> GetByCodeAsync(string transactionCode)
        {
            string query = "SELECT * FROM transaction WHERE transaction_code = @code";
            var parameters = new NpgsqlParameter[] { new("@code", transactionCode) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Transaction
            {
                TransactionId = Convert.ToInt32(row["transaction_id"]),
                StudentId = Convert.ToInt32(row["student_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                TransactionCode = row["transaction_code"]?.ToString(),
                TransactionDateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]),
                TransactionRemarks = row["transaction_remarks"]?.ToString(),
                TransactionDateAdded = Convert.ToDateTime(row["transaction_date_added"]),
                TransactionLastUpdated = Convert.ToDateTime(row["transaction_last_updated"])
            };
        }

        public async Task<IEnumerable<Transaction>> GetByStudentIdAsync(int studentId)
        {
            string query = "SELECT * FROM transaction WHERE student_id = @studentId ORDER BY transaction_id DESC";
            var parameters = new NpgsqlParameter[] { new("@studentId", studentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new Transaction
            {
                TransactionId = Convert.ToInt32(row["transaction_id"]),
                StudentId = Convert.ToInt32(row["student_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                TransactionCode = row["transaction_code"]?.ToString(),
                TransactionDateBorrowed = Convert.ToDateTime(row["transaction_date_borrowed"]),
                TransactionRemarks = row["transaction_remarks"]?.ToString(),
                TransactionDateAdded = Convert.ToDateTime(row["transaction_date_added"]),
                TransactionLastUpdated = Convert.ToDateTime(row["transaction_last_updated"])
            });
        }

        public async Task<bool> AddAsync(Transaction transaction)
        {
            string query = @"INSERT INTO transaction 
                (student_id, user_id, transaction_code, transaction_date_borrowed, transaction_remarks)
                VALUES (@studentId, @userId, @code, @dateBorrowed, @remarks)";

            var parameters = new NpgsqlParameter[]
            {
                new("@studentId", transaction.StudentId),
                new("@userId", transaction.UserId),
                new("@code", (object?)transaction.TransactionCode ?? DBNull.Value),
                new("@dateBorrowed", transaction.TransactionDateBorrowed),
                new("@remarks", (object?)transaction.TransactionRemarks ?? DBNull.Value)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Transaction transaction)
        {
            string query = @"UPDATE transaction SET 
                student_id = @studentId, user_id = @userId,
                transaction_code = @code, transaction_date_borrowed = @dateBorrowed,
                transaction_remarks = @remarks, transaction_last_updated = CURRENT_TIMESTAMP
                WHERE transaction_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", transaction.TransactionId),
                new("@studentId", transaction.StudentId),
                new("@userId", transaction.UserId),
                new("@code", (object?)transaction.TransactionCode ?? DBNull.Value),
                new("@dateBorrowed", transaction.TransactionDateBorrowed),
                new("@remarks", (object?)transaction.TransactionRemarks ?? DBNull.Value)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int transactionId)
        {
            string query = "DELETE FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int transactionId)
        {
            string query = "SELECT COUNT(1) FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
