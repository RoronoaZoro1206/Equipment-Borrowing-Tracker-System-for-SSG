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
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseHelper _db;

        public UserRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            string query = "SELECT * FROM \"USER\" ORDER BY user_id";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new User
            {
                UserId = Convert.ToInt32(row["user_id"]),
                UserUsername = row["user_username"].ToString() ?? "",
                UserHashedPassword = row["user_hashed_password"].ToString() ?? "",
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
            });
        }


        public async Task<User?> GetByIdAsync(int userId)
        {
            string query = "SELECT * FROM \"USER\" WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new User
            {
                UserId = Convert.ToInt32(row["user_id"]),
                UserUsername = row["user_username"].ToString() ?? "",
                UserHashedPassword = row["user_hashed_password"].ToString() ?? "",
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

        public async Task<User?> GetByUsernameAsync(string username)
        {
            string query = "SELECT * FROM \"USER\" WHERE user_username = @username";
            var parameters = new NpgsqlParameter[] { new("@username", username) };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new User
            {
                UserId = Convert.ToInt32(row["user_id"]),
                UserUsername = row["user_username"].ToString() ?? "",
                UserHashedPassword = row["user_hashed_password"].ToString() ?? "",
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

        public async Task<bool> AddAsync(User user)
        {
            string query = @"INSERT INTO ""USER"" 
                (user_username, user_hashed_password, user_role, user_position, 
                 user_last_name, user_first_name, user_middle_name, user_suffix, 
                 user_email, user_contact_no, user_status)
                VALUES (@username, @password, @role::user_role_enum, @position, 
                        @lastName, @firstName, @middleName, @suffix, 
                        @email, @contactNo, @status)";

            var parameters = new NpgsqlParameter[]
            {
                new("@username", user.UserUsername),
                new("@password", user.UserHashedPassword),
                new("@role", user.UserRole),
                new("@position", user.UserPosition),
                new("@lastName", user.UserLastName),
                new("@firstName", user.UserFirstName),
                new("@middleName", (object?)user.UserMiddleName ?? DBNull.Value),
                new("@suffix", (object?)user.UserSuffix ?? DBNull.Value),
                new("@email", (object?)user.UserEmail ?? DBNull.Value),
                new("@contactNo", (object?)user.UserContactNo ?? DBNull.Value),
                new("@status", user.UserStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(User user)
        {
            string query = @"UPDATE ""USER"" SET 
                user_username = @username, user_hashed_password = @password,
                user_role = @role::user_role_enum, user_position = @position,
                user_last_name = @lastName, user_first_name = @firstName,
                user_middle_name = @middleName, user_suffix = @suffix,
                user_email = @email, user_contact_no = @contactNo,
                user_status = @status, user_last_updated = CURRENT_TIMESTAMP
                WHERE user_id = @id";

            var parameters = new NpgsqlParameter[]
            {
                new("@id", user.UserId),
                new("@username", user.UserUsername),
                new("@password", user.UserHashedPassword),
                new("@role", user.UserRole),
                new("@position", user.UserPosition),
                new("@lastName", user.UserLastName),
                new("@firstName", user.UserFirstName),
                new("@middleName", (object?)user.UserMiddleName ?? DBNull.Value),
                new("@suffix", (object?)user.UserSuffix ?? DBNull.Value),
                new("@email", (object?)user.UserEmail ?? DBNull.Value),
                new("@contactNo", (object?)user.UserContactNo ?? DBNull.Value),
                new("@status", user.UserStatus)
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            string query = "DELETE FROM \"USER\" WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int userId)
        {
            string query = "SELECT COUNT(1) FROM \"USER\" WHERE user_id = @id";
            var parameters = new NpgsqlParameter[] { new("@id", userId) };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }
    }
}
