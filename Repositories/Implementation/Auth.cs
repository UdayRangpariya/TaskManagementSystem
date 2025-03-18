using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interface;
using Repositories.Model;

namespace Repositories.Implementation
{
    public class Auth : IAuthInterface
    {
        private readonly string _connectionString;

        public Auth(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<User> RegisterUserAsync(Register registerModel)
        {
            // Hash the password
            string passwordHash = HashPassword(registerModel.Password);

            // Create connection and command
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Check if username or email already exists
                using (var checkCommand = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM t_users WHERE c_username = @username OR c_email = @email", connection))
                {
                    checkCommand.Parameters.AddWithValue("username", registerModel.Username);
                    checkCommand.Parameters.AddWithValue("email", registerModel.Email);

                    long count = (long)await checkCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        throw new Exception("Username or email already exists");
                    }
                }

                // Insert new user
                using (var command = new NpgsqlCommand(
                    @"INSERT INTO t_users (c_username, c_email, c_password_hash, c_first_name, c_last_name, c_profile_picture) 
                      VALUES (@username, @email, @passwordHash, @firstName, @lastName, @profilePicture) 
                      RETURNING c_user_id, c_username, c_email, c_role, c_first_name, c_last_name, 
                      c_profile_picture, c_created_at, c_is_active", connection))
                {
                    command.Parameters.AddWithValue("username", registerModel.Username);
                    command.Parameters.AddWithValue("email", registerModel.Email);
                    command.Parameters.AddWithValue("passwordHash", passwordHash);
                    command.Parameters.AddWithValue("firstName", registerModel.FirstName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("lastName", registerModel.LastName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("profilePicture", registerModel.ProfilePicture ?? (object)DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = Enum.Parse<user_role>(reader.GetString(3)),
                                FirstName = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                LastName = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                ProfilePicture = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                                CreatedAt = reader.GetDateTime(7),
                                IsActive = reader.GetBoolean(8)
                            };
                        }
                    }
                }
            }

            throw new Exception("Failed to register user");
        }

        public async Task<User> LoginUserAsync(Login loginModel)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql;
                NpgsqlCommand command;

                // If role is specified, include it in the query
                if (!string.IsNullOrEmpty(loginModel.role))
                {
                    sql = "SELECT c_user_id, c_username, c_email, c_password_hash, c_role, c_first_name, c_last_name, " +
                          "c_profile_picture, c_created_at, c_is_active FROM t_users " +
                          "WHERE c_username = @username AND c_role::text = @role";

                    command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("username", loginModel.Username);
                    command.Parameters.AddWithValue("role", loginModel.role.ToLower());
                }
                else
                {
                    sql = "SELECT c_user_id, c_username, c_email, c_password_hash, c_role, c_first_name, c_last_name, " +
                          "c_profile_picture, c_created_at, c_is_active FROM t_users WHERE c_username = @username";

                    command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("username", loginModel.Username);
                }

                using (command)
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int userId = reader.GetInt32(0);
                            string username = reader.GetString(1);
                            string email = reader.GetString(2);
                            string storedHash = reader.GetString(3);
                            string roleString = reader.GetString(4);
                            string firstName = !reader.IsDBNull(5) ? reader.GetString(5) : null;
                            string lastName = !reader.IsDBNull(6) ? reader.GetString(6) : null;
                            string profilePicture = !reader.IsDBNull(7) ? reader.GetString(7) : null;
                            DateTime createdAt = reader.GetDateTime(8);
                            bool isActive = reader.GetBoolean(9);

                            // Verify password
                            if (VerifyPassword(loginModel.Password, storedHash))
                            {
                                var user = new User
                                {
                                    UserId = userId,
                                    Username = username,
                                    Email = email,
                                    PasswordHash = storedHash,
                                    Role = Enum.Parse<user_role>(roleString),
                                    FirstName = firstName,
                                    LastName = lastName,
                                    ProfilePicture = profilePicture,
                                    CreatedAt = createdAt,
                                    IsActive = isActive
                                };

                                // Close the reader before executing another command on the same connection
                                reader.Close();

                                // Update last login time
                                await UpdateLastLoginAsync(connection, user.UserId);

                                return user;
                            }
                        }
                    }
                }
            }

            throw new Exception("Invalid username, password, or role");
        }

        // Only the UpdateLastLoginAsync method needs a fix
        private async Task UpdateLastLoginAsync(NpgsqlConnection existingConnection, int userId)
        {
            // Use the existing connection instead of creating a new one
            using (var command = new NpgsqlCommand(
                "UPDATE t_users SET c_last_login = CURRENT_TIMESTAMP WHERE c_user_id = @userId", existingConnection))
            {
                command.Parameters.AddWithValue("userId", userId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT c_user_id, c_username, c_email, c_role, c_first_name, c_last_name,
                    c_profile_picture, c_created_at, c_last_login, c_is_active
                    FROM t_users
                    ORDER BY c_created_at DESC";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = Enum.Parse<user_role>(reader.GetString(3)),
                                FirstName = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                LastName = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                ProfilePicture = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                                CreatedAt = reader.GetDateTime(7),
                                LastLogin = !reader.IsDBNull(8) ? reader.GetDateTime(8) : (DateTime?)null,
                                IsActive = reader.GetBoolean(9)
                            });
                        }
                    }
                }
            }

            return users;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT c_user_id, c_username, c_email, c_password_hash, c_role, c_first_name, c_last_name,
                    c_profile_picture, c_created_at, c_last_login, c_is_active
                    FROM t_users
                    WHERE c_user_id = @userId";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("userId", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                PasswordHash = reader.GetString(3),
                                Role = Enum.Parse<user_role>(reader.GetString(4)),
                                FirstName = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                LastName = !reader.IsDBNull(6) ? reader.GetString(6) : null,
                                ProfilePicture = !reader.IsDBNull(7) ? reader.GetString(7) : null,
                                CreatedAt = reader.GetDateTime(8),
                                LastLogin = !reader.IsDBNull(9) ? reader.GetDateTime(9) : (DateTime?)null,
                                IsActive = reader.GetBoolean(10)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<User> UpdateUserAsync(int userId, UserUpdate userUpdate)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // First check if user exists
                string checkSql = "SELECT COUNT(*) FROM t_users WHERE c_user_id = @userId";

                using (var checkCommand = new NpgsqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("userId", userId);

                    long count = (long)await checkCommand.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        return null; // User not found
                    }
                }

                // Build the update SQL dynamically based on which fields are provided
                StringBuilder sqlBuilder = new StringBuilder("UPDATE t_users SET c_updated_at = CURRENT_TIMESTAMP");

                if (!string.IsNullOrEmpty(userUpdate.FirstName))
                {
                    sqlBuilder.Append(", c_first_name = @firstName");
                }

                if (!string.IsNullOrEmpty(userUpdate.LastName))
                {
                    sqlBuilder.Append(", c_last_name = @lastName");
                }

                if (!string.IsNullOrEmpty(userUpdate.Email))
                {
                    sqlBuilder.Append(", c_email = @email");
                }

                if (!string.IsNullOrEmpty(userUpdate.ProfilePicture))
                {
                    sqlBuilder.Append(", c_profile_picture = @profilePicture");
                }

                if (userUpdate.Role.HasValue)
                {
                    sqlBuilder.Append(", c_role = @role");
                }

                if (userUpdate.IsActive.HasValue)
                {
                    sqlBuilder.Append(", c_is_active = @isActive");
                }

                sqlBuilder.Append(" WHERE c_user_id = @userId");

                string sql = sqlBuilder.ToString();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("userId", userId);

                    if (!string.IsNullOrEmpty(userUpdate.FirstName))
                    {
                        command.Parameters.AddWithValue("firstName", userUpdate.FirstName);
                    }

                    if (!string.IsNullOrEmpty(userUpdate.LastName))
                    {
                        command.Parameters.AddWithValue("lastName", userUpdate.LastName);
                    }

                    if (!string.IsNullOrEmpty(userUpdate.Email))
                    {
                        command.Parameters.AddWithValue("email", userUpdate.Email);
                    }

                    if (!string.IsNullOrEmpty(userUpdate.ProfilePicture))
                    {
                        command.Parameters.AddWithValue("profilePicture", userUpdate.ProfilePicture);
                    }

                    if (userUpdate.Role.HasValue)
                    {
                        command.Parameters.AddWithValue("role", userUpdate.Role.Value.ToString().ToLower());
                    }

                    if (userUpdate.IsActive.HasValue)
                    {
                        command.Parameters.AddWithValue("isActive", userUpdate.IsActive.Value);
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }

            // Return the updated user
            return await GetUserByIdAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // First verify current password
                string checkSql = "SELECT c_password_hash FROM t_users WHERE c_user_id = @userId";

                string storedHash;
                using (var checkCommand = new NpgsqlCommand(checkSql, connection))
                {
                    checkCommand.Parameters.AddWithValue("userId", userId);

                    var result = await checkCommand.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        return false; // User not found
                    }

                    storedHash = result.ToString();
                }

                if (!VerifyPassword(currentPassword, storedHash))
                {
                    return false; // Current password is incorrect
                }

                // Update password
                string newPasswordHash = HashPassword(newPassword);
                string updateSql = "UPDATE t_users SET c_password_hash = @passwordHash WHERE c_user_id = @userId";

                using (var updateCommand = new NpgsqlCommand(updateSql, connection))
                {
                    updateCommand.Parameters.AddWithValue("passwordHash", newPasswordHash);
                    updateCommand.Parameters.AddWithValue("userId", userId);

                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Delete user sessions
                        string deleteSessionsSql = "DELETE FROM t_user_sessions WHERE c_user_id = @userId";
                        using (var command = new NpgsqlCommand(deleteSessionsSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("userId", userId);
                            await command.ExecuteNonQueryAsync();
                        }

                        // Delete user
                        string deleteUserSql = "DELETE FROM t_users WHERE c_user_id = @userId";
                        using (var command = new NpgsqlCommand(deleteUserSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("userId", userId);
                            int rowsAffected = await command.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<SystemStatistics> GetSystemStatisticsAsync()
        {
            var stats = new SystemStatistics();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get user statistics
                string userStatsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM t_users) AS total_users,
                        (SELECT COUNT(*) FROM t_users WHERE c_is_active = true) AS active_users,
                        (SELECT COUNT(*) FROM t_users WHERE c_role = 'admin') AS admin_users";

                using (var command = new NpgsqlCommand(userStatsSql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats.TotalUsers = reader.GetInt32(0);
                            stats.ActiveUsers = reader.GetInt32(1);
                            stats.AdminUsers = reader.GetInt32(2);
                        }
                    }
                }

                // Get task statistics
                string taskStatsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM t_tasks) AS total_tasks,
                        (SELECT COUNT(*) FROM t_tasks WHERE c_status = 'pending') AS pending_tasks,
                        (SELECT COUNT(*) FROM t_tasks WHERE c_status = 'in_progress') AS in_progress_tasks,
                        (SELECT COUNT(*) FROM t_tasks WHERE c_status = 'completed') AS completed_tasks";

                using (var command = new NpgsqlCommand(taskStatsSql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats.TotalTasks = reader.GetInt32(0);
                            stats.PendingTasks = reader.GetInt32(1);
                            stats.InProgressTasks = reader.GetInt32(2);
                            stats.CompletedTasks = reader.GetInt32(3);
                        }
                    }
                }

                // Get message statistics
                string messageStatsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM t_messages) AS total_messages,
                        (SELECT COUNT(*) FROM t_messages WHERE c_is_read = false) AS unread_messages";

                using (var command = new NpgsqlCommand(messageStatsSql, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats.TotalMessages = reader.GetInt32(0);
                            stats.UnreadMessages = reader.GetInt32(1);
                        }
                    }
                }
            }

            return stats;
        }

        // Existing helper methods: UpdateLastLoginAsync, HashPassword, VerifyPassword
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            string hashedPassword = HashPassword(password);
            return hashedPassword == storedHash;
        }
    }
}