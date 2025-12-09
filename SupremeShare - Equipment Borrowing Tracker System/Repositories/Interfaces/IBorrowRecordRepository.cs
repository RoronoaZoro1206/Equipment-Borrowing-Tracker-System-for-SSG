using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IBorrowRecordRepository
    {
        Task<IEnumerable<BorrowRecord>> GetAllAsync();
        Task<BorrowRecord?> GetByIdAsync(int borrowId);
        Task<IEnumerable<BorrowRecord>> GetByTransactionIdAsync(int transactionId);
        Task<IEnumerable<BorrowRecord>> GetOverdueRecordsAsync();
        Task<bool> AddAsync(BorrowRecord borrowRecord);
        Task<bool> UpdateAsync(BorrowRecord borrowRecord);
        Task<bool> DeleteAsync(int borrowId);
        Task<bool> ExistsAsync(int borrowId);
        
        /// <summary>
        /// Calculates days overdue using database function fn_calculate_days_overdue.
        /// </summary>
        Task<int> CalculateDaysOverdueAsync(int borrowId);
        
        /// <summary>
        /// Counts borrow records by status using database function fn_count_borrow_status.
        /// Valid statuses: 'borrowed', 'returned', 'overdue', 'lost'
        /// </summary>
        Task<int> CountByStatusAsync(string status);

        /// <summary>
        /// Process borrow using sp_process_borrow_transaction (auto-selects available unit by equipment type)
        /// </summary>
        Task ProcessBorrowByEquipmentAsync(int studentId, int userId, int equipmentId, DateTime dueDate);

        /// <summary>
        /// Process return using sp_process_return_boolean.
        /// If condition is FALSE (Damaged), unit status becomes 'for review'.
        /// </summary>
        /// <param name="borrowId">The borrow record ID</param>
        /// <param name="returnCondition">TRUE = Good (available), FALSE = Damaged (for review)</param>
        /// <param name="userId">The user processing the return</param>
        /// <returns>A result indicating if the item was sent for review</returns>
        Task<ReturnProcessResult> ProcessReturnBooleanAsync(int borrowId, bool returnCondition, int userId);
    }

    /// <summary>
    /// Result of processing a return operation
    /// </summary>
    public class ReturnProcessResult
    {
        public bool Success { get; set; }
        public bool SentForReview { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
