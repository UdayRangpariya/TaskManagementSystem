using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interface;
using Repositories.Model;

namespace Repositories.Implementation
{
    public class Auth : IAuth
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
                                Role = Enum.Parse<UserRole>(reader.GetString(3)),
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

                // Get user by username
                using (var command = new NpgsqlCommand(
                    "SELECT c_user_id, c_username, c_email, c_password_hash, c_role, c_first_name, c_last_name, " +
                    "c_profile_picture, c_created_at, c_is_active FROM t_users WHERE c_username = @username", connection))
                {
                    command.Parameters.AddWithValue("username", loginModel.Username);

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
                                    Role = Enum.Parse<UserRole>(roleString),
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

            throw new Exception("Invalid username or password");
        }

        private async Task UpdateLastLoginAsync(NpgsqlConnection connection, int userId)
        {
            // Create a new connection instead of reusing the existing one
            using (var newConnection = new NpgsqlConnection(_connectionString))
            {
                await newConnection.OpenAsync();
                using (var command = new NpgsqlCommand(
                    "UPDATE t_users SET c_last_login = CURRENT_TIMESTAMP WHERE c_user_id = @userId", newConnection))
                {
                    command.Parameters.AddWithValue("userId", userId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

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