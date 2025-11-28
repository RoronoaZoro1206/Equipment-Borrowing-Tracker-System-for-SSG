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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<TransactionRepository> _logger;

        public TransactionRepository(DatabaseHelper db, IAuditService auditService, ILogger<TransactionRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            string query = "SELECT * FROM transaction ORDER BY transaction_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToTransaction(row));
        }

        public async Task<Transaction?> GetByIdAsync(int transactionId)
        {
            InputValidator.ValidateId(transactionId, "Transaction ID");

            string query = "SELECT * FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToTransaction(dt.Rows[0]);
        }

        public async Task<Transaction?> GetByCodeAsync(string transactionCode)
        {
            var sanitizedCode = InputValidator.SanitizeString(transactionCode, 100);
            if (string.IsNullOrWhiteSpace(sanitizedCode))
                throw new ValidationException("Transaction code is required.");

            string query = "SELECT * FROM transaction WHERE transaction_code = @code";
            var parameters = new NpgsqlParameter[] { new("@code", sanitizedCode) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToTransaction(dt.Rows[0]);
        }

        public async Task<IEnumerable<Transaction>> GetByStudentIdAsync(int studentId)
        {
            InputValidator.ValidateId(studentId, "Student ID");

            string query = "SELECT * FROM transaction WHERE student_id = @studentId ORDER BY transaction_id DESC";
            var parameters = new NpgsqlParameter[] { new("@studentId", studentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => MapToTransaction(row));
        }

        public async Task<bool> AddAsync(Transaction transaction)
        {
            ValidateTransaction(transaction);

            string query = @"INSERT INTO transaction 
                (student_id, user_id, transaction_code, transaction_date_borrowed, transaction_remarks)
                VALUES (@studentId, @userId, @code, @dateBorrowed, @remarks)";

            var parameters = new NpgsqlParameter[]
            {
                new("@studentId", transaction.StudentId),
                new("@userId", transaction.UserId),
                new("@code", (object?)InputValidator.SanitizeString(transaction.TransactionCode, 100) ?? DBNull.Value),
                new("@dateBorrowed", transaction.TransactionDateBorrowed),
                new("@remarks", (object?)InputValidator.SanitizeString(transaction.TransactionRemarks, 500) ?? DBNull.Value)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(transaction.UserId, "TRANSACTION_CREATED", null,
                    new { transaction.TransactionCode, transaction.StudentId, transaction.TransactionDateBorrowed });
                _logger.LogInformation("Transaction created: {TransactionCode}", transaction.TransactionCode);
            }

            return result;
        }


        public async Task<bool> UpdateAsync(Transaction transaction)
        {
            InputValidator.ValidateId(transaction.TransactionId, "Transaction ID");
            ValidateTransaction(transaction);

            var existingTransaction = await GetByIdAsync(transaction.TransactionId);
            if (existingTransaction == null)
                throw new NotFoundException("Transaction not found.");

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
                new("@code", (object?)InputValidator.SanitizeString(transaction.TransactionCode, 100) ?? DBNull.Value),
                new("@dateBorrowed", transaction.TransactionDateBorrowed),
                new("@remarks", (object?)InputValidator.SanitizeString(transaction.TransactionRemarks, 500) ?? DBNull.Value)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(transaction.UserId, "TRANSACTION_UPDATED",
                    new { existingTransaction.TransactionCode },
                    new { transaction.TransactionCode, transaction.TransactionRemarks });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int transactionId)
        {
            InputValidator.ValidateId(transactionId, "Transaction ID");

            var existingTransaction = await GetByIdAsync(transactionId);
            if (existingTransaction == null)
                throw new NotFoundException("Transaction not found.");

            // Check for active borrow records
            string checkQuery = "SELECT COUNT(1) FROM borrow_record WHERE transaction_id = @id AND borrow_status NOT IN ('returned', 'lost')";
            var checkParams = new NpgsqlParameter[] { new("@id", transactionId) };
            var activeRecords = Convert.ToInt32(await _db.ExecuteScalarAsync(checkQuery, checkParams));
            
            if (activeRecords > 0)
                throw new ValidationException("Cannot delete transaction with active borrow records.");

            string query = "DELETE FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "TRANSACTION_DELETED",
                    new { existingTransaction.TransactionCode }, null);
                _logger.LogWarning("Transaction deleted: {TransactionId}", transactionId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int transactionId)
        {
            InputValidator.ValidateId(transactionId, "Transaction ID");

            string query = "SELECT COUNT(1) FROM transaction WHERE transaction_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", transactionId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        private void ValidateTransaction(Transaction transaction)
        {
            InputValidator.ValidateId(transaction.StudentId, "Student ID");
            InputValidator.ValidateId(transaction.UserId, "User ID");
            if (transaction.TransactionDateBorrowed == default)
                throw new ValidationException("Transaction date borrowed is required.");
        }

        private Transaction MapToTransaction(DataRow row)
        {
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
    }
}
