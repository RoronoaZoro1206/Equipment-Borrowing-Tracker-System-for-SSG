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
    public class BorrowRecordRepository : IBorrowRecordRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<BorrowRecordRepository> _logger;

        public BorrowRecordRepository(DatabaseHelper db, IAuditService auditService, ILogger<BorrowRecordRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<BorrowRecord>> GetAllAsync()
        {
            string query = "SELECT * FROM borrow_record ORDER BY borrow_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToBorrowRecord(row));
        }

        public async Task<BorrowRecord?> GetByIdAsync(int borrowId)
        {
            InputValidator.ValidateId(borrowId, "Borrow ID");

            string query = "SELECT * FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToBorrowRecord(dt.Rows[0]);
        }

        public async Task<IEnumerable<BorrowRecord>> GetByTransactionIdAsync(int transactionId)
        {
            InputValidator.ValidateId(transactionId, "Transaction ID");

            string query = "SELECT * FROM borrow_record WHERE transaction_id = @transactionId ORDER BY borrow_id";
            var parameters = new NpgsqlParameter[] { new("@transactionId", transactionId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => MapToBorrowRecord(row));
        }

        public async Task<IEnumerable<BorrowRecord>> GetOverdueRecordsAsync()
        {
            string query = "SELECT * FROM borrow_record WHERE borrow_status = 'overdue' ORDER BY borrow_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToBorrowRecord(row));
        }

        public async Task<bool> AddAsync(BorrowRecord borrowRecord)
        {
            ValidateBorrowRecord(borrowRecord);

            string query = @"INSERT INTO borrow_record 
                (transaction_id, eunit_id, borrow_due_date, borrow_date_returned, 
                 borrow_return_condition, borrow_status)
                VALUES (@transactionId, @eunitId, @dueDate, @dateReturned, @returnCondition, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@transactionId", borrowRecord.TransactionId),
                new("@eunitId", borrowRecord.EunitId),
                new("@dueDate", borrowRecord.BorrowDueDate),
                new("@dateReturned", (object?)borrowRecord.BorrowDateReturned ?? DBNull.Value),
                new("@returnCondition", (object?)borrowRecord.BorrowReturnCondition ?? DBNull.Value),
                new("@status", borrowRecord.BorrowStatus.ToLower())
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "BORROW_RECORD_CREATED", null,
                    new { borrowRecord.TransactionId, borrowRecord.EunitId, borrowRecord.BorrowStatus });
                _logger.LogInformation("Borrow record created for transaction: {TransactionId}", borrowRecord.TransactionId);
            }

            return result;
        }


        public async Task<bool> UpdateAsync(BorrowRecord borrowRecord)
        {
            InputValidator.ValidateId(borrowRecord.BorrowId, "Borrow ID");
            ValidateBorrowRecord(borrowRecord);

            var existingRecord = await GetByIdAsync(borrowRecord.BorrowId);
            if (existingRecord == null)
                throw new NotFoundException("Borrow record not found.");

            string query = @"UPDATE borrow_record SET 
                transaction_id = @transactionId, eunit_id = @eunitId,
                borrow_due_date = @dueDate, borrow_date_returned = @dateReturned,
                borrow_return_condition = @returnCondition, borrow_status = @status,
                borrow_last_updated = CURRENT_TIMESTAMP
                WHERE borrow_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", borrowRecord.BorrowId),
                new("@transactionId", borrowRecord.TransactionId),
                new("@eunitId", borrowRecord.EunitId),
                new("@dueDate", borrowRecord.BorrowDueDate),
                new("@dateReturned", (object?)borrowRecord.BorrowDateReturned ?? DBNull.Value),
                new("@returnCondition", (object?)borrowRecord.BorrowReturnCondition ?? DBNull.Value),
                new("@status", borrowRecord.BorrowStatus.ToLower())
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "BORROW_RECORD_UPDATED",
                    new { existingRecord.BorrowStatus },
                    new { borrowRecord.BorrowStatus, borrowRecord.BorrowDateReturned });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int borrowId)
        {
            InputValidator.ValidateId(borrowId, "Borrow ID");

            var existingRecord = await GetByIdAsync(borrowId);
            if (existingRecord == null)
                throw new NotFoundException("Borrow record not found.");

            // Prevent deletion of active borrow records
            if (existingRecord.BorrowStatus.ToLower() == "borrowed")
                throw new ValidationException("Cannot delete an active borrow record.");

            string query = "DELETE FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "BORROW_RECORD_DELETED",
                    new { existingRecord.BorrowId, existingRecord.BorrowStatus }, null);
                _logger.LogWarning("Borrow record deleted: {BorrowId}", borrowId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int borrowId)
        {
            InputValidator.ValidateId(borrowId, "Borrow ID");

            string query = "SELECT COUNT(1) FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<int> CalculateDaysOverdueAsync(int borrowId)
        {
            InputValidator.ValidateId(borrowId, "Borrow ID");

            string query = "SELECT fn_calculate_days_overdue(@id)";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Counts borrow records by status using database function fn_count_borrow_status.
        /// Valid statuses: 'borrowed', 'returned', 'overdue', 'lost'
        /// </summary>
        public async Task<int> CountByStatusAsync(string status)
        {
            var validStatuses = new[] { "borrowed", "returned", "overdue", "lost" };
            var normalizedStatus = status?.ToLower() ?? "";
            
            if (!validStatuses.Contains(normalizedStatus))
                throw new ValidationException($"Invalid borrow status. Valid values: {string.Join(", ", validStatuses)}");

            string query = "SELECT fn_count_borrow_status(@status)";
            var parameters = new NpgsqlParameter[] { new("@status", normalizedStatus) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Process borrow using sp_process_borrow_transaction (auto-selects available unit by equipment type)
        /// </summary>
        public async Task ProcessBorrowByEquipmentAsync(int studentId, int userId, int equipmentId, DateTime dueDate)
        {
            InputValidator.ValidateId(studentId, "Student ID");
            InputValidator.ValidateId(userId, "User ID");
            InputValidator.ValidateId(equipmentId, "Equipment ID");

            string query = "CALL sp_process_borrow_transaction(@p_student_id, @p_user_id, @p_equipment_id, @p_due_date)";
            var parameters = new NpgsqlParameter[]
            {
                new("@p_student_id", studentId),
                new("@p_user_id", userId),
                new("@p_equipment_id", equipmentId),
                new("@p_due_date", dueDate)
            };

            await _db.ExecuteNonQueryAsync(query, parameters);
            _logger.LogInformation("Borrow processed via sp_process_borrow_transaction: Student {StudentId}, Equipment {EquipmentId}", studentId, equipmentId);
        }

        /// <summary>
        /// Process return using sp_process_return_transaction (VARCHAR condition: 'Good' or 'Damaged')
        /// </summary>
        public async Task ProcessReturnWithConditionAsync(int borrowId, string condition)
        {
            InputValidator.ValidateId(borrowId, "Borrow ID");
            
            if (string.IsNullOrWhiteSpace(condition) || (condition != "Good" && condition != "Damaged"))
                throw new ValidationException("Condition must be 'Good' or 'Damaged'");

            string query = "CALL sp_process_return_transaction(@p_borrow_id, @p_return_condition)";
            var parameters = new NpgsqlParameter[]
            {
                new("@p_borrow_id", borrowId),
                new("@p_return_condition", condition)
            };

            await _db.ExecuteNonQueryAsync(query, parameters);
            _logger.LogInformation("Return processed via sp_process_return_transaction: BorrowId {BorrowId}, Condition {Condition}", borrowId, condition);
        }

        private void ValidateBorrowRecord(BorrowRecord record)
        {
            InputValidator.ValidateId(record.TransactionId, "Transaction ID");
            InputValidator.ValidateId(record.EunitId, "Equipment Unit ID");
            InputValidator.ValidateBorrowStatus(record.BorrowStatus);
            if (record.BorrowDueDate == default)
                throw new ValidationException("Borrow due date is required.");
            if (record.BorrowDueDate < DateTime.Now.AddHours(-1))
                throw new ValidationException("Borrow due date cannot be in the past.");
        }

        private BorrowRecord MapToBorrowRecord(DataRow row)
        {
            return new BorrowRecord
            {
                BorrowId = Convert.ToInt32(row["borrow_id"]),
                TransactionId = Convert.ToInt32(row["transaction_id"]),
                EunitId = Convert.ToInt32(row["eunit_id"]),
                BorrowDueDate = Convert.ToDateTime(row["borrow_due_date"]),
                BorrowDateReturned = row["borrow_date_returned"] == DBNull.Value ? null : Convert.ToDateTime(row["borrow_date_returned"]),
                BorrowReturnCondition = row["borrow_return_condition"] == DBNull.Value ? null : Convert.ToBoolean(row["borrow_return_condition"]),
                BorrowStatus = row["borrow_status"].ToString() ?? "",
                BorrowDateAdded = Convert.ToDateTime(row["borrow_date_added"]),
                BorrowLastUpdated = Convert.ToDateTime(row["borrow_last_updated"])
            };
        }
    }
}
