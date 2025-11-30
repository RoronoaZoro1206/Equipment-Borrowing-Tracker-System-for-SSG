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

        // POST: SupportAdmin/Students/Create
        // Support Admin can add new students for borrowing purposes
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] SupportStudentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.StudentIdNumber))
                    return Json(new { success = false, message = "Student ID number is required" });
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return Json(new { success = false, message = "First name is required" });
                if (string.IsNullOrWhiteSpace(request.LastName))
                    return Json(new { success = false, message = "Last name is required" });
                if (string.IsNullOrWhiteSpace(request.College))
                    return Json(new { success = false, message = "College is required" });

                // Check if ID number exists
                var existing = await _studentRepo.GetByIdNumberAsync(request.StudentIdNumber);
                if (existing != null)
                    return Json(new { success = false, message = "Student ID number already exists" });

                var student = new Models.Student
                {
                    StudentIdNumber = request.StudentIdNumber,
                    StudentFirstName = request.FirstName,
                    StudentLastName = request.LastName,
                    StudentMiddleName = request.MiddleName,
                    StudentSuffix = request.Suffix,
                    StudentCollege = request.College,
                    StudentCourse = request.Course,
                    StudentEmail = request.Email,
                    StudentContactNo = request.ContactNo,
                    StudentStatus = true
                };

                var result = await _studentRepo.AddAsync(student);
                if (result)
                {
                    _logger.LogInformation("Student created by Support Admin: {StudentIdNumber}", request.StudentIdNumber);
                    return Json(new { success = true, message = "Student created successfully" });
                }

                return Json(new { success = false, message = "Failed to create student" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: SupportAdmin/Students/Update
        // Support Admin can update student info (but not status)
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] SupportStudentRequest request)
        {
            try
            {
                if (request.StudentId <= 0)
                    return Json(new { success = false, message = "Invalid student ID" });

                var student = await _studentRepo.GetByIdAsync(request.StudentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                // Check if new ID number conflicts
                if (request.StudentIdNumber != student.StudentIdNumber)
                {
                    var existing = await _studentRepo.GetByIdNumberAsync(request.StudentIdNumber);
                    if (existing != null && existing.StudentId != request.StudentId)
                        return Json(new { success = false, message = "Student ID number already in use" });
                }

                student.StudentIdNumber = request.StudentIdNumber ?? student.StudentIdNumber;
                student.StudentFirstName = request.FirstName ?? student.StudentFirstName;
                student.StudentLastName = request.LastName ?? student.StudentLastName;
                student.StudentMiddleName = request.MiddleName;
                student.StudentSuffix = request.Suffix;
                student.StudentCollege = request.College ?? student.StudentCollege;
                student.StudentCourse = request.Course;
                student.StudentEmail = request.Email;
                student.StudentContactNo = request.ContactNo;
                // Note: Support Admin cannot change student status

                var result = await _studentRepo.UpdateAsync(student);
                if (result)
                {
                    _logger.LogInformation("Student updated by Support Admin: {StudentId}", request.StudentId);
                }
                return Json(new { success = result, message = result ? "Student updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class SupportStudentRequest
    {
        public int StudentId { get; set; }
        public string StudentIdNumber { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? Suffix { get; set; }
        public string? College { get; set; }
        public string? Course { get; set; }
        public string? Email { get; set; }
        public string? ContactNo { get; set; }
    }
}
