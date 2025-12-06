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
    }
}
