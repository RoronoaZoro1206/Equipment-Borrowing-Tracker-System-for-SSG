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
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseHelper _db;
        private readonly IAuditService _auditService;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(DatabaseHelper db, IAuditService auditService, ILogger<UserRepository> logger)
        {
            _db = db;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            // Note: Password hash is excluded from general queries for security
            string query = @"SELECT user_id, user_username, user_role, user_position, 
                user_last_name, user_first_name, user_middle_name, user_suffix,
                user_email, user_contact_no, user_status, user_date_created, user_last_updated 
                FROM ""USER"" ORDER BY user_id";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => MapToUser(row, excludePassword: true));
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            InputValidator.ValidateId(userId, "User ID");

            string query = @"SELECT user_id, user_username, user_role, user_position, 
                user_last_name, user_first_name, user_middle_name, user_suffix,
                user_email, user_contact_no, user_status, user_date_created, user_last_updated 
                FROM ""USER"" WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToUser(dt.Rows[0], excludePassword: true);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var sanitizedUsername = InputValidator.SanitizeString(username, 50);
            InputValidator.ValidateUsername(sanitizedUsername);

            string query = "SELECT * FROM \"USER\" WHERE user_username = @username";
            var parameters = new NpgsqlParameter[] { new("@username", sanitizedUsername) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;
            return MapToUser(dt.Rows[0], excludePassword: false);
        }


        public async Task<bool> AddAsync(User user)
        {
            ValidateUser(user);

            string query = @"INSERT INTO ""USER"" 
                (user_username, user_hashed_password, user_role, user_position, 
                 user_last_name, user_first_name, user_middle_name, user_suffix, 
                 user_email, user_contact_no, user_status)
                VALUES (@username, @password, @role::user_role_enum, @position, 
                        @lastName, @firstName, @middleName, @suffix, 
                        @email, @contactNo, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@username", InputValidator.SanitizeString(user.UserUsername, 50)),
                new("@password", user.UserHashedPassword), // Already hashed
                new("@role", user.UserRole.ToLower()),
                new("@position", InputValidator.SanitizeString(user.UserPosition, 100)),
                new("@lastName", InputValidator.SanitizeString(user.UserLastName, 100)),
                new("@firstName", InputValidator.SanitizeString(user.UserFirstName, 100)),
                new("@middleName", (object?)InputValidator.SanitizeString(user.UserMiddleName, 100) ?? DBNull.Value),
                new("@suffix", (object?)InputValidator.SanitizeString(user.UserSuffix, 20) ?? DBNull.Value),
                new("@email", (object?)InputValidator.SanitizeString(user.UserEmail, 100) ?? DBNull.Value),
                new("@contactNo", (object?)InputValidator.SanitizeString(user.UserContactNo, 20) ?? DBNull.Value),
                new("@status", user.UserStatus)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(0, "USER_CREATED", null, new { user.UserUsername, user.UserRole });
                _logger.LogInformation("User created: {Username}", user.UserUsername);
            }

            return result;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            InputValidator.ValidateId(user.UserId, "User ID");
            ValidateUser(user);

            // Get existing user for audit
            var existingUser = await GetByIdAsync(user.UserId);
            if (existingUser == null)
                throw new NotFoundException("User not found.");

            string query = @"UPDATE ""USER"" SET 
                user_username = @username, user_role = @role::user_role_enum, 
                user_position = @position, user_last_name = @lastName, 
                user_first_name = @firstName, user_middle_name = @middleName, 
                user_suffix = @suffix, user_email = @email, user_contact_no = @contactNo,
                user_status = @status, user_last_updated = CURRENT_TIMESTAMP
                WHERE user_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", user.UserId),
                new("@username", InputValidator.SanitizeString(user.UserUsername, 50)),
                new("@role", user.UserRole.ToLower()),
                new("@position", InputValidator.SanitizeString(user.UserPosition, 100)),
                new("@lastName", InputValidator.SanitizeString(user.UserLastName, 100)),
                new("@firstName", InputValidator.SanitizeString(user.UserFirstName, 100)),
                new("@middleName", (object?)InputValidator.SanitizeString(user.UserMiddleName, 100) ?? DBNull.Value),
                new("@suffix", (object?)InputValidator.SanitizeString(user.UserSuffix, 20) ?? DBNull.Value),
                new("@email", (object?)InputValidator.SanitizeString(user.UserEmail, 100) ?? DBNull.Value),
                new("@contactNo", (object?)InputValidator.SanitizeString(user.UserContactNo, 20) ?? DBNull.Value),
                new("@status", user.UserStatus)
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(user.UserId, "USER_UPDATED", 
                    new { existingUser.UserUsername, existingUser.UserRole },
                    new { user.UserUsername, user.UserRole });
            }

            return result;
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            InputValidator.ValidateId(userId, "User ID");

            var existingUser = await GetByIdAsync(userId);
            if (existingUser == null)
                throw new NotFoundException("User not found.");

            // Soft delete - set status to false instead of actual deletion
            string query = @"UPDATE ""USER"" SET user_status = FALSE, 
                user_last_updated = CURRENT_TIMESTAMP WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            
            var result = await _db.ExecuteNonQueryAsync(query, parameters) > 0;
            
            if (result)
            {
                await _auditService.LogActionAsync(userId, "USER_DEACTIVATED", 
                    new { existingUser.UserUsername }, null);
                _logger.LogWarning("User deactivated: {UserId}", userId);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(int userId)
        {
            InputValidator.ValidateId(userId, "User ID");

            string query = "SELECT COUNT(1) FROM \"USER\" WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        private void ValidateUser(User user)
        {
            InputValidator.ValidateUsername(user.UserUsername);
            InputValidator.ValidateRole(user.UserRole);
            InputValidator.ValidateName(user.UserLastName, "Last name");
            InputValidator.ValidateName(user.UserFirstName, "First name");
            InputValidator.ValidateName(user.UserMiddleName, "Middle name", required: false);
            InputValidator.ValidateEmail(user.UserEmail);
            InputValidator.ValidatePhone(user.UserContactNo);
        }

        private User MapToUser(DataRow row, bool excludePassword)
        {
            return new User
            {
                UserId = Convert.ToInt32(row["user_id"]),
                UserUsername = row["user_username"].ToString() ?? "",
                UserHashedPassword = excludePassword ? "" : row["user_hashed_password"]?.ToString() ?? "",
                UserRole = row["user_role"].ToString() ?? "",
                UserPosition = row["user_position"].ToString() ?? "",
                UserLastName = row["user_last_name"].ToString() ?? "",
                UserFirstName = row["user_first_name"].ToString() ?? "",
                UserMiddleName = row["user_middle_name"]?.ToString(),
                UserSuffix = row["user_suffix"]?.ToString(),
                UserEmail = row["user_email"]?.ToString(),
                UserContactNo = row["user_contact_no"]?.ToString(),
                UserStatus = Convert.ToBoolean(row["user_status"]),
                UserDateCreated = Convert.ToDateTime(row["user_date_created"]),
                UserLastUpdated = Convert.ToDateTime(row["user_last_updated"])
            };
        }
    }
}
