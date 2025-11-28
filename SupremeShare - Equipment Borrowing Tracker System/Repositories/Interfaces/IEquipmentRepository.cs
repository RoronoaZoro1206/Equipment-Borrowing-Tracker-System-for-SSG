using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IEquipmentRepository
    {
        Task<IEnumerable<Equipment>> GetAllAsync();
        Task<Equipment?> GetByIdAsync(int equipmentId);
        Task<bool> AddAsync(Equipment equipment);
        Task<bool> UpdateAsync(Equipment equipment);
        Task<bool> DeleteAsync(int equipmentId);
        Task<bool> ExistsAsync(int equipmentId);
        Task<int> GetAvailableStockAsync(int equipmentId);
    }
}
