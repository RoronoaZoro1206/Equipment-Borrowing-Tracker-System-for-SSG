using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.SupportAdmin
{
    [Route("SupportAdmin/Students")]
    [SupportAdmin]
    public class SupportAdminStudentController : Controller
    {
        private readonly IStudentRepository _studentRepo;
        private readonly ILogger<SupportAdminStudentController> _logger;

        public SupportAdminStudentController(
            IStudentRepository studentRepo,
            ILogger<SupportAdminStudentController> logger)
        {
            _studentRepo = studentRepo;
            _logger = logger;
        }

        // GET: SupportAdmin/Students (View-Only)
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var students = await _studentRepo.GetAllAsync();
            return View("~/Views/SupportAdmin/Students/Index.cshtml", students);
        }

        // GET: SupportAdmin/Students/GetAll (API)
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var students = await _studentRepo.GetAllAsync();
                var result = students.Select(s => new
                {
                    s.StudentId,
                    s.StudentIdNumber,
                    s.FullName,
                    s.StudentCollege,
                    s.StudentCourse,
                    s.StudentEmail,
                    s.StudentContactNo,
                    s.StudentStatus,
                    StudentDateAdded = s.StudentDateAdded.ToString("yyyy-MM-dd")
                });
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students");
                return Json(new { success = false, message = "Failed to fetch students" });
            }
        }

        // GET: SupportAdmin/Students/Get/{id}
        [HttpGet("Get/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var student = await _studentRepo.GetByIdAsync(id);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                return Json(new { success = true, data = student });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student {StudentId}", id);
                return Json(new { success = false, message = "Failed to fetch student" });
            }
        }

        // GET: SupportAdmin/Students/Search?q=xxx
        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Json(new { success = true, data = Array.Empty<object>() });

                // Search by ID number first
                var byIdNumber = await _studentRepo.GetByIdNumberAsync(q);
                if (byIdNumber != null)
                {
                    return Json(new { success = true, data = new[] { new
                    {
                        byIdNumber.StudentId,
                        byIdNumber.StudentIdNumber,
                        byIdNumber.FullName,
                        byIdNumber.StudentCollege,
                        byIdNumber.StudentCourse,
                        byIdNumber.StudentStatus
                    }}});
                }

                // Search by name
                var allStudents = await _studentRepo.GetAllAsync();
                var filtered = allStudents
                    .Where(s => s.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                s.StudentIdNumber.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Take(20)
                    .Select(s => new
                    {
                        s.StudentId,
                        s.StudentIdNumber,
                        s.FullName,
                        s.StudentCollege,
                        s.StudentCourse,
                        s.StudentStatus
                    });

                return Json(new { success = true, data = filtered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching students");
                return Json(new { success = false, message = "Search failed" });
            }
        }

        // Note: Support Admin cannot Create, Update, Delete, or Toggle Status of students
        // These operations are Admin-only
    }
}
