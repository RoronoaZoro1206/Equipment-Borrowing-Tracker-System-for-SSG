using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using SupremeShare___Equipment_Borrowing_Tracker_System.Data;
using SupremeShare___Equipment_Borrowing_Tracker_System.Models;
using SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Interfaces;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly DatabaseHelper _db;

        public StudentRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            string query = "SELECT * FROM student ORDER BY student_id";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Student
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
            });
        }


        public async Task<Student?> GetByIdAsync(int studentId)
        {
            string query = "SELECT * FROM student WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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

        public async Task<Student?> GetByIdNumberAsync(string studentIdNumber)
        {
            string query = "SELECT * FROM student WHERE student_id_number = @idNumber";
            var parameters = new NpgsqlParameter[] { new("@idNumber", studentIdNumber) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
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

        public async Task<bool> AddAsync(Student student)
        {
            string query = @"INSERT INTO student 
                (student_id_number, student_last_name, student_first_name, student_middle_name,
                 student_suffix, student_college, student_course, student_email, 
                 student_contact_no, student_status)
                VALUES (@idNumber, @lastName, @firstName, @middleName,
                        @suffix, @college, @course, @email, @contactNo, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@idNumber", student.StudentIdNumber),
                new("@lastName", student.StudentLastName),
                new("@firstName", student.StudentFirstName),
                new("@middleName", (object?)student.StudentMiddleName ?? DBNull.Value),
                new("@suffix", (object?)student.StudentSuffix ?? DBNull.Value),
                new("@college", student.StudentCollege),
                new("@course", (object?)student.StudentCourse ?? DBNull.Value),
                new("@email", (object?)student.StudentEmail ?? DBNull.Value),
                new("@contactNo", (object?)student.StudentContactNo ?? DBNull.Value),
                new("@status", student.StudentStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Student student)
        {
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
                new("@idNumber", student.StudentIdNumber),
                new("@lastName", student.StudentLastName),
                new("@firstName", student.StudentFirstName),
                new("@middleName", (object?)student.StudentMiddleName ?? DBNull.Value),
                new("@suffix", (object?)student.StudentSuffix ?? DBNull.Value),
                new("@college", student.StudentCollege),
                new("@course", (object?)student.StudentCourse ?? DBNull.Value),
                new("@email", (object?)student.StudentEmail ?? DBNull.Value),
                new("@contactNo", (object?)student.StudentContactNo ?? DBNull.Value),
                new("@status", student.StudentStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int studentId)
        {
            string query = "DELETE FROM student WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int studentId)
        {
            string query = "SELECT COUNT(1) FROM student WHERE student_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", studentId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
