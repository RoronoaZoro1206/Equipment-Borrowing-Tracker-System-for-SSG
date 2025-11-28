using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;
using SupremeShare___Equipment_Borrowing_Tracker_System.Exceptions;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<StudentRepository> _logger;

        public StudentRepository(DatabaseHelper db, IAuditService auditService, ILogger<StudentRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            string query = "SELECT * FROM student ORDER BY student_id";
            var dt = await _db.ExecuteQueryAsync(query);
            return dt.AsEnumerable().Select(row => MapToStudent(row));
        }

        public async Task<Student?> GetByIdAsync(int studentId)
        {
            InputValidator.ValidateId(studentId, "Student ID");

            string query = "SELECT * FROM student WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToStudent(dt.Rows[0]);
        }

        public async Task<Student?> GetByIdNumberAsync(string studentIdNumber)
        {
            var sanitizedIdNumber = InputValidator.SanitizeString(studentIdNumber, 20);
            if (string.IsNullOrWhiteSpace(sanitizedIdNumber))
                throw new ValidationException("Student ID number is required.");

            string query = "SELECT * FROM student WHERE student_id_number = @idNumber";
            var parameters = new NpgsqlParameter[] { new("@idNumber", sanitizedIdNumber) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToStudent(dt.Rows[0]);
        }


        public async Task<bool> AddAsync(Student student)
        {
            ValidateStudent(student);

            string query = @"INSERT INTO student 
                (student_id_number, student_last_name, student_first_name, student_middle_name,
                 student_suffix, student_college, student_course, student_email, 
                 student_contact_no, student_status)
                VALUES (@idNumber, @lastName, @firstName, @middleName,
                        @suffix, @college, @course, @email, @contactNo, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@idNumber", InputValidator.SanitizeString(student.StudentIdNumber, 20)),
                new("@lastName", InputValidator.SanitizeString(student.StudentLastName, 100)),
                new("@firstName", InputValidator.SanitizeString(student.StudentFirstName, 100)),
                new("@middleName", (object?)InputValidator.SanitizeString(student.StudentMiddleName, 100) ?? DBNull.Value),
                new("@suffix", (object?)InputValidator.SanitizeString(student.StudentSuffix, 20) ?? DBNull.Value),
                new("@college", InputValidator.SanitizeString(student.StudentCollege, 150)),
                new("@course", (object?)InputValidator.SanitizeString(student.StudentCourse, 100) ?? DBNull.Value),
                new("@email", (object?)InputValidator.SanitizeString(student.StudentEmail, 100) ?? DBNull.Value),
                new("@contactNo", (object?)InputValidator.SanitizeString(student.StudentContactNo, 20) ?? DBNull.Value),
                new("@status", student.StudentStatus)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "STUDENT_CREATED", null, 
                    new { student.StudentIdNumber, student.StudentLastName, student.StudentFirstName });
                _logger.LogInformation("Student created: {StudentIdNumber}", student.StudentIdNumber);
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Student student)
        {
            InputValidator.ValidateId(student.StudentId, "Student ID");
            ValidateStudent(student);

            var existingStudent = await GetByIdAsync(student.StudentId);
            if (existingStudent == null)
                throw new NotFoundException("Student not found.");

            string query = @"UPDATE student SET 
                student_id_number = @idNumber, student_last_name = @lastName,
                student_first_name = @firstName, student_middle_name = @middleName,
                student_suffix = @suffix, student_college = @college,
                student_course = @course, student_email = @email,
                student_contact_no = @contactNo, student_status = @status,
                student_last_updated = CURRENT_TIMESTAMP
                WHERE student_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", student.StudentId),
                new("@idNumber", InputValidator.SanitizeString(student.StudentIdNumber, 20)),
                new("@lastName", InputValidator.SanitizeString(student.StudentLastName, 100)),
                new("@firstName", InputValidator.SanitizeString(student.StudentFirstName, 100)),
                new("@middleName", (object?)InputValidator.SanitizeString(student.StudentMiddleName, 100) ?? DBNull.Value),
                new("@suffix", (object?)InputValidator.SanitizeString(student.StudentSuffix, 20) ?? DBNull.Value),
                new("@college", InputValidator.SanitizeString(student.StudentCollege, 150)),
                new("@course", (object?)InputValidator.SanitizeString(student.StudentCourse, 100) ?? DBNull.Value),
                new("@email", (object?)InputValidator.SanitizeString(student.StudentEmail, 100) ?? DBNull.Value),
                new("@contactNo", (object?)InputValidator.SanitizeString(student.StudentContactNo, 20) ?? DBNull.Value),
                new("@status", student.StudentStatus)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "STUDENT_UPDATED",
                    new { existingStudent.StudentIdNumber },
                    new { student.StudentIdNumber });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int studentId)
        {
            InputValidator.ValidateId(studentId, "Student ID");

            var existingStudent = await GetByIdAsync(studentId);
            if (existingStudent == null)
                throw new NotFoundException("Student not found.");

            // Soft delete
            string query = @"UPDATE student SET student_status = FALSE, 
                student_last_updated = CURRENT_TIMESTAMP WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "STUDENT_DEACTIVATED",
                    new { existingStudent.StudentIdNumber }, null);
                _logger.LogWarning("Student deactivated: {StudentId}", studentId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int studentId)
        {
            InputValidator.ValidateId(studentId, "Student ID");

            string query = "SELECT COUNT(1) FROM student WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        private void ValidateStudent(Student student)
        {
            if (string.IsNullOrWhiteSpace(student.StudentIdNumber))
                throw new ValidationException("Student ID number is required.");
            InputValidator.ValidateName(student.StudentLastName, "Last name");
            InputValidator.ValidateName(student.StudentFirstName, "First name");
            InputValidator.ValidateName(student.StudentMiddleName, "Middle name", required: false);
            if (string.IsNullOrWhiteSpace(student.StudentCollege))
                throw new ValidationException("College is required.");
            InputValidator.ValidateEmail(student.StudentEmail);
            InputValidator.ValidatePhone(student.StudentContactNo);
        }

        private Student MapToStudent(DataRow row)
        {
            return new Student
            {
                StudentId = Convert.ToInt32(row["student_id"]),
                StudentIdNumber = row["student_id_number"].ToString() ?? "",
                StudentLastName = row["student_last_name"].ToString() ?? "",
                StudentFirstName = row["student_first_name"].ToString() ?? "",
                StudentMiddleName = row["student_middle_name"]?.ToString(),
                StudentSuffix = row["student_suffix"]?.ToString(),
                StudentCollege = row["student_college"].ToString() ?? "",
                StudentCourse = row["student_course"]?.ToString(),
                StudentEmail = row["student_email"]?.ToString(),
                StudentContactNo = row["student_contact_no"]?.ToString(),
                StudentStatus = Convert.ToBoolean(row["student_status"]),
                StudentDateAdded = Convert.ToDateTime(row["student_date_added"]),
                StudentLastUpdated = Convert.ToDateTime(row["student_last_updated"])
            };
        }
    }
}
