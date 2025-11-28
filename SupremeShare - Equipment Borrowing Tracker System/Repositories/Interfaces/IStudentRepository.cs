using System.Collections.Generic;
using System.Threading.Tasks;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces
{
    public interface IStudentRepository
    {
        Task<IEnumerable<Student>> GetAllAsync();
        Task<Student?> GetByIdAsync(int studentId);
        Task<Student?> GetByIdNumberAsync(string studentIdNumber);
        Task<bool> AddAsync(Student student);
        Task<bool> UpdateAsync(Student student);
        Task<bool> DeleteAsync(int studentId);
        Task<bool> ExistsAsync(int studentId);
    }
}
