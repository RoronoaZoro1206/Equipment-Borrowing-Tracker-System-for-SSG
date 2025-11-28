using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<AuditLog?> GetByIdAsync(int auditId);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
        Task<bool> AddAsync(AuditLog auditLog);
        Task<bool> DeleteAsync(int auditId);
        Task<bool> ExistsAsync(int auditId);
    }
}
