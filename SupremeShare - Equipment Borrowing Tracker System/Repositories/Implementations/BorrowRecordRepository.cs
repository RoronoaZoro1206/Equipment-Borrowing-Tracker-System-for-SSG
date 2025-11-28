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
    public class BorrowRecordRepository : IBorrowRecordRepository
    {
        private readonly DatabaseHelper _db;

        public BorrowRecordRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BorrowRecord>> GetAllAsync()
        {
            string query = "SELECT * FROM borrow_record ORDER BY borrow_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new BorrowRecord
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
            });
        }

        public async Task<BorrowRecord?> GetByIdAsync(int borrowId)
        {
            string query = "SELECT * FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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


        public async Task<IEnumerable<BorrowRecord>> GetByTransactionIdAsync(int transactionId)
        {
            string query = "SELECT * FROM borrow_record WHERE transaction_id = @transactionId ORDER BY borrow_id";
            var parameters = new NpgsqlParameter[] { new("@transactionId", transactionId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            return dt.AsEnumerable().Select(row => new BorrowRecord
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
            });
        }

        public async Task<IEnumerable<BorrowRecord>> GetOverdueRecordsAsync()
        {
            string query = "SELECT * FROM borrow_record WHERE borrow_status = 'overdue' ORDER BY borrow_id DESC";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new BorrowRecord
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
            });
        }

        public async Task<bool> AddAsync(BorrowRecord borrowRecord)
        {
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
                new("@status", borrowRecord.BorrowStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(BorrowRecord borrowRecord)
        {
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
                new("@status", borrowRecord.BorrowStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int borrowId)
        {
            string query = "DELETE FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int borrowId)
        {
            string query = "SELECT COUNT(1) FROM borrow_record WHERE borrow_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        public async Task<int> CalculateDaysOverdueAsync(int borrowId)
        {
            string query = "SELECT fn_calculate_days_overdue(@id)";
            var parameters = new NpgsqlParameter[] { new("@id", borrowId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result);
        }
    }
}
