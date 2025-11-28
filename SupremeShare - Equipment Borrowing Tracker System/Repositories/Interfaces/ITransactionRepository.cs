using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<Transaction>> GetAllAsync();
        Task<Transaction?> GetByIdAsync(int transactionId);
        Task<Transaction?> GetByCodeAsync(string transactionCode);
        Task<IEnumerable<Transaction>> GetByStudentIdAsync(int studentId);
        Task<bool> AddAsync(Transaction transaction);
        Task<bool> UpdateAsync(Transaction transaction);
        Task<bool> DeleteAsync(int transactionId);
        Task<bool> ExistsAsync(int transactionId);
    }
}
