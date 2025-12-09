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

        /// <summary>
        /// Gets all equipment units with status 'for review' (returned damaged, pending admin decision)
        /// </summary>
        Task<IEnumerable<EquipmentUnitForReview>> GetUnitsForReviewAsync();

        /// <summary>
        /// Admin processes a unit review using sp_update_unit_condition.
        /// TRUE = Good (moves to 'available'), FALSE = Damaged (moves to 'maintenance')
        /// </summary>
        /// <param name="unitId">The equipment unit ID</param>
        /// <param name="condition">TRUE = Good, FALSE = Damaged</param>
        /// <param name="userId">The admin user ID</param>
        Task<ReviewProcessResult> ProcessUnitReviewAsync(int unitId, bool condition, int userId);
    }

    /// <summary>
    /// Equipment unit pending admin review with additional details
    /// </summary>
    public class EquipmentUnitForReview
    {
        public int EunitId { get; set; }
        public int EquipmentId { get; set; }
        public string EunitSerialNo { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string EquipmentCategory { get; set; } = string.Empty;
        public DateTime ReturnedDate { get; set; }
        public string? ReturnedByStudentName { get; set; }
        public string? ReturnedByStudentIdNumber { get; set; }
        public DateTime EunitLastUpdated { get; set; }
    }

    /// <summary>
    /// Result of processing a unit review
    /// </summary>
    public class ReviewProcessResult
    {
        public bool Success { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
