using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Data
{
    /// <summary>
    /// One-time service to migrate plaintext passwords to BCrypt hashes.
    /// Run this once after updating PasswordHasher to BCrypt.
    /// </summary>
    public class PasswordMigrationService
    {
        private readonly DatabaseHelper _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<PasswordMigrationService> _logger;

        public PasswordMigrationService(
            DatabaseHelper db,
            IPasswordHasher passwordHasher,
            ILogger<PasswordMigrationService> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Migrates all plaintext passwords to BCrypt hashes.
        /// Only processes passwords that don't start with $2 (BCrypt prefix).
        /// </summary>
        public async Task MigratePasswordsAsync()
        {
            _logger.LogInformation("Starting password migration to BCrypt...");

            try
            {
                // Find users with plaintext passwords (not BCrypt format)
                var query = @"SELECT user_id, user_username, user_hashed_password 
                              FROM ""USER"" 
                              WHERE user_hashed_password NOT LIKE '$2a$%' 
                                AND user_hashed_password NOT LIKE '$2b$%' 
                                AND user_hashed_password NOT LIKE '$2y$%'";

                var dt = await _db.ExecuteQueryAsync(query);

                if (dt.Rows.Count == 0)
                {
                    _logger.LogInformation("No plaintext passwords found. All passwords are already hashed.");
                    return;
                }

                _logger.LogWarning("Found {Count} user(s) with plaintext passwords. Migrating...", dt.Rows.Count);

                int successCount = 0;
                int failCount = 0;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    var userId = Convert.ToInt32(row["user_id"]);
                    var username = row["user_username"].ToString() ?? "";
                    var plaintextPassword = row["user_hashed_password"].ToString() ?? "";

                    try
                    {
                        // Hash the plaintext password
                        var hashedPassword = _passwordHasher.HashPassword(plaintextPassword);

                        // Update the user's password
                        var updateQuery = @"UPDATE ""USER"" 
                                           SET user_hashed_password = @hashedPassword,
                                               user_last_updated = CURRENT_TIMESTAMP
                                           WHERE user_id = @userId";

                        var parameters = new NpgsqlParameter[]
                        {
                            new("@hashedPassword", hashedPassword),
                            new("@userId", userId)
                        };

                        await _db.ExecuteNonQueryAsync(updateQuery, parameters);

                        _logger.LogInformation("✓ Migrated password for user: {Username} (ID: {UserId})", username, userId);
                        successCount++;
                    }
                    catch (ArgumentException ex)
                    {
                        // Password validation failed (too short, etc.)
                        _logger.LogError("✗ Failed to migrate user {Username}: {Error}", username, ex.Message);
                        failCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "✗ Failed to migrate user {Username}", username);
                        failCount++;
                    }
                }

                _logger.LogInformation("═══════════════════════════════════════════");
                _logger.LogInformation("Password Migration Complete!");
                _logger.LogInformation("  Successful: {Success}", successCount);
                _logger.LogInformation("  Failed:     {Failed}", failCount);
                _logger.LogInformation("═══════════════════════════════════════════");

                // Clean up migration flag if it exists
                try
                {
                    var cleanupQuery = @"UPDATE ""USER"" SET password_needs_migration = FALSE 
                                        WHERE password_needs_migration = TRUE";
                    await _db.ExecuteNonQueryAsync(cleanupQuery);
                }
                catch
                {
                    // Column might not exist, ignore
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password migration failed!");
                throw;
            }
        }

        /// <summary>
        /// Checks if any users have plaintext passwords that need migration
        /// </summary>
        public async Task<bool> HasPlaintextPasswordsAsync()
        {
            var query = @"SELECT COUNT(1) FROM ""USER"" 
                         WHERE user_hashed_password NOT LIKE '$2a$%' 
                           AND user_hashed_password NOT LIKE '$2b$%' 
                           AND user_hashed_password NOT LIKE '$2y$%'";

            var result = await _db.ExecuteScalarAsync(query);
            return Convert.ToInt32(result) > 0;
        }
    }
}
