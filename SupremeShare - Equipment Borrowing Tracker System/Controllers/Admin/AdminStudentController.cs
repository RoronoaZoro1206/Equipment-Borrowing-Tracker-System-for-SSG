using Microsoft.AspNetCore.Mvc;
using SupremeShare___Equipment_Borrowing_Tracker_System.Attributes;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Services;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Controllers.Admin
{
    [Route("Admin/Students")]
    [AdminOnly]
    public class AdminStudentController : Controller
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IAuditService _auditService;
        private readonly IDashboardNotificationService _notificationService;
        private readonly ILogger<AdminStudentController> _logger;

        public AdminStudentController(
            IStudentRepository studentRepo,
            IAuditService auditService,
            IDashboardNotificationService notificationService,
            ILogger<AdminStudentController> logger)
        {
            _studentRepo = studentRepo;
            _auditService = auditService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Admin/Students
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var students = await _studentRepo.GetAllAsync();
            return View("~/Views/Admin/Students/Index.cshtml", students);
        }

        // GET: Admin/Students/GetAll (API)
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

        // GET: Admin/Students/Get/{id}
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

        // GET: Admin/Students/Search?q=xxx
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

                // Search by name (get all and filter)
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

        // POST: Admin/Students/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] StudentRequest request)
        {
            try
            {
                // Validate
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

                var student = new Student
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
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    _logger.LogInformation("Student created: {StudentIdNumber} by user {UserId}", request.StudentIdNumber, userId);
                    
                    await _auditService.LogActionAsync(userId, "STUDENT_CREATED", null, new { request.StudentIdNumber, request.FirstName, request.LastName });
                    await _notificationService.NotifyStudentUpdated($"{request.FirstName} {request.LastName}", "CREATED");
                    
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

        // POST: Admin/Students/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromForm] StudentRequest request)
        {
            try
            {
                if (request.StudentId <= 0)
                    return Json(new { success = false, message = "Invalid student ID" });

                var student = await _studentRepo.GetByIdAsync(request.StudentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                // Check if new ID number conflicts with another student
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

                var result = await _studentRepo.UpdateAsync(student);
                if (result)
                {
                    var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    await _auditService.LogActionAsync(userId, "STUDENT_UPDATED", null, new { student.StudentIdNumber, student.FullName });
                    await _notificationService.NotifyStudentUpdated(student.FullName, "UPDATED");
                }
                return Json(new { success = result, message = result ? "Student updated" : "Update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Students/ToggleStatus
        [HttpPost("ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus([FromForm] int studentId)
        {
            try
            {
                var student = await _studentRepo.GetByIdAsync(studentId);
                if (student == null)
                    return Json(new { success = false, message = "Student not found" });

                var oldStatus = student.StudentStatus;
                student.StudentStatus = !student.StudentStatus;
                var result = await _studentRepo.UpdateAsync(student);

                var action = student.StudentStatus ? "activated" : "deactivated";
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _logger.LogInformation("Student {StudentId} {Action} by user {UserId}", studentId, action, userId);

                if (result)
                {
                    await _auditService.LogActionAsync(userId, $"STUDENT_{action.ToUpper()}", 
                        new { Status = oldStatus }, new { Status = student.StudentStatus });
                    await _notificationService.NotifyStudentUpdated(student.FullName, action.ToUpper());
                }

                return Json(new { success = result, message = $"Student {action}", status = student.StudentStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling student status");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Students/Delete
        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int studentId)
        {
            try
            {
                var result = await _studentRepo.DeleteAsync(studentId);
                return Json(new { success = result, message = result ? "Student deactivated" : "Delete failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class StudentRequest
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
