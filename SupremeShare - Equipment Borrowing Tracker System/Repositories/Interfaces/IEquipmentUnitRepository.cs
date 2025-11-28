using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IEquipmentUnitRepository
    {
        Task<IEnumerable<EquipmentUnit>> GetAllAsync();
        Task<EquipmentUnit?> GetByIdAsync(int eunitId);
        Task<EquipmentUnit?> GetBySerialNoAsync(string serialNo);
        Task<IEnumerable<EquipmentUnit>> GetByEquipmentIdAsync(int equipmentId);
        Task<IEnumerable<EquipmentUnit>> GetAvailableUnitsAsync(int equipmentId);
        Task<bool> AddAsync(EquipmentUnit equipmentUnit);
        Task<bool> UpdateAsync(EquipmentUnit equipmentUnit);
        Task<bool> DeleteAsync(int eunitId);
        Task<bool> ExistsAsync(int eunitId);
    }
}
