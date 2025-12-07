using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> AddAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> UpdatePasswordAsync(int userId, string hashedPassword);
        Task<bool> DeleteAsync(int userId);
        Task<bool> ExistsAsync(int userId);
        
        // Role-based queries for Admin management workflow
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<IEnumerable<User>> GetActiveByRoleAsync(string role);
        Task<IEnumerable<User>> GetInactiveByRoleAsync(string role);
        Task<bool> ActivateUserAsync(int userId, string hashedPassword);
        Task<bool> DeactivateUserAsync(int userId);
    }
}
