using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Repositories.Interface;
using Repositories.Model.AdminModels;

namespace Repositories.Implementation.AdminRepo
{
    public class AdminRepo : AdminInterface
    {
        private readonly NpgsqlConnection _conn;

        public AdminRepo(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        #region GetAllUsers
        public async Task<List<UserModel>> GetAllUsers()
        {
            var query = "SELECT * FROM t_users WHERE c_role = 'user'";
            List<UserModel> userList = new List<UserModel>();

            try
            {
                await _conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        UserModel model = new UserModel
                        {
                            c_user_id = reader.GetInt32(reader.GetOrdinal("c_user_id")),
                            c_username = reader.GetString(reader.GetOrdinal("c_username")),
                            c_email = reader.GetString(reader.GetOrdinal("c_email")),
                            c_password_hash = reader.GetString(reader.GetOrdinal("c_password_hash")),
                            c_role = Enum.Parse<user_role>(reader.GetString(reader.GetOrdinal("c_role"))),
                            c_first_name = reader.GetString(reader.GetOrdinal("c_first_name")),
                            c_last_name = reader.GetString(reader.GetOrdinal("c_last_name")),
                            c_profile_picture = reader.IsDBNull(reader.GetOrdinal("c_profile_picture"))
                                                ? null
                                                : reader.GetString(reader.GetOrdinal("c_profile_picture")),
                            c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                            c_last_login = reader.IsDBNull(reader.GetOrdinal("c_last_login"))
                                           ? null
                                           : reader.GetDateTime(reader.GetOrdinal("c_last_login")),
                            c_is_active = reader.GetBoolean(reader.GetOrdinal("c_is_active"))
                        };

                        userList.Add(model);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching users: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return userList;
        }

        #endregion 

        #region GetUserById

        public async Task<UserModel?> GetUserById(int userId)
        {
            var query = "SELECT * FROM t_users WHERE c_user_id = @UserId";

            try
            {
                await _conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        return new UserModel
                        {
                            c_user_id = reader.GetInt32(reader.GetOrdinal("c_user_id")),
                            c_username = reader.GetString(reader.GetOrdinal("c_username")),
                            c_email = reader.GetString(reader.GetOrdinal("c_email")),
                            c_password_hash = reader.GetString(reader.GetOrdinal("c_password_hash")),
                            c_role = Enum.Parse<user_role>(reader.GetString(reader.GetOrdinal("c_role"))),
                            c_first_name = reader.GetString(reader.GetOrdinal("c_first_name")),
                            c_last_name = reader.GetString(reader.GetOrdinal("c_last_name")),
                            c_profile_picture = reader.IsDBNull(reader.GetOrdinal("c_profile_picture"))
                                                ? null
                                                : reader.GetString(reader.GetOrdinal("c_profile_picture")),
                            c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                            c_last_login = reader.IsDBNull(reader.GetOrdinal("c_last_login"))
                                           ? null
                                           : reader.GetDateTime(reader.GetOrdinal("c_last_login")),
                            c_is_active = reader.GetBoolean(reader.GetOrdinal("c_is_active"))
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return null; // Return null if user is not found
        }

        #endregion

        #region GetAllTasks
        public async Task<List<TaskModel>> GetAllTasks()
        {
            var query = "SELECT * FROM t_tasks";
            List<TaskModel> taskList = new List<TaskModel>();

            try
            {
                await _conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        taskList.Add(new TaskModel
                        {
                            c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                            c_title = reader.GetString(reader.GetOrdinal("c_title")),
                            c_description = reader.GetString(reader.GetOrdinal("c_description")),
                            c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                            c_priority = reader.IsDBNull(reader.GetOrdinal("c_priority")) ? null : reader.GetInt32(reader.GetOrdinal("c_priority")),
                            c_due_date = reader.IsDBNull(reader.GetOrdinal("c_due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                            c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                            c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                            c_created_by = reader.IsDBNull(reader.GetOrdinal("c_created_by")) ? null : reader.GetInt32(reader.GetOrdinal("c_created_by")),
                            c_assigned_to = reader.IsDBNull(reader.GetOrdinal("c_assigned_to")) ? null : reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tasks: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return taskList;
        }

        #endregion
    

        #region GetTaskbyUserId
        public async Task<List<TaskModel>> GetTasksByUserId(int userId)
        {
            var query = "SELECT * FROM t_tasks WHERE c_created_by = @UserId";
            List<TaskModel> taskList = new List<TaskModel>();

            try
            {
                await _conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        taskList.Add(new TaskModel
                        {
                            c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                            c_title = reader.GetString(reader.GetOrdinal("c_title")),
                            c_description = reader.GetString(reader.GetOrdinal("c_description")),
                            c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                            c_priority = reader.IsDBNull(reader.GetOrdinal("c_priority")) ? null : reader.GetInt32(reader.GetOrdinal("c_priority")),
                            c_due_date = reader.IsDBNull(reader.GetOrdinal("c_due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                            c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                            c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                            c_created_by = reader.IsDBNull(reader.GetOrdinal("c_created_by")) ? null : reader.GetInt32(reader.GetOrdinal("c_created_by")),
                            c_assigned_to = reader.IsDBNull(reader.GetOrdinal("c_assigned_to")) ? null : reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tasks for user {userId}: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return taskList;
        }


        #endregion
        #region Assigned Task
        public async Task<int> AssignTask(TaskModel task)
        {
            var query = @"INSERT INTO t_tasks 
        (c_title, c_description, c_status, c_priority, c_due_date, c_created_at, c_updated_at, c_created_by, c_assigned_to)
        VALUES (@Title, @Description,@Status::task_status , @Priority, @DueDate, @CreatedAt, @UpdatedAt, @CreatedBy, @AssignedTo)
     RETURNING c_task_id";
            Console.WriteLine(task.c_status.ToString());
            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {


                    cmd.Parameters.AddWithValue("@Title", task.c_title);
                    cmd.Parameters.AddWithValue("@Description", task.c_description);

                    // Convert Enum to String
                    // cmd.Parameters.AddWithValue("@Status", task.c_status.ToString());
                    // cmd.Parameters.AddWithValue("@Status", task.c_status);
                    cmd.Parameters.AddWithValue("@Status", task.c_status.ToString());
                    // cmd.Parameters.AddWithValue("@Status", task.c_status);
                    cmd.Parameters.AddWithValue("@Priority", (object?)task.c_priority ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DueDate", (object?)task.c_due_date ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", task.c_created_at);
                    cmd.Parameters.AddWithValue("@UpdatedAt", task.c_updated_at);
                    cmd.Parameters.AddWithValue("@CreatedBy", (object?)task.c_created_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@AssignedTo", (object?)task.c_assigned_to ?? DBNull.Value);

                    int rowsAffected = (int)await cmd.ExecuteScalarAsync();
                    Console.WriteLine($"Task added successfully with ID: {rowsAffected}");
                    return rowsAffected;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting task: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        #endregion

        #region DeleteRask
        public async Task<(int c_created_by, int c_assigned_to, string c_title)> DeleteTaskAsync(int taskId)
        {

            Console.WriteLine($"task id from the t_task reop {taskId}");
            var query = "DELETE FROM t_tasks WHERE c_task_id = @TaskId RETURNING c_created_by, c_assigned_to, c_title;";

            try
            {
                await _conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@TaskId", taskId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Extract values from the deleted row
                            int userId = reader.GetInt32(reader.GetOrdinal("c_created_by"));
                            int relatedId = reader.GetInt32(reader.GetOrdinal("c_assigned_to"));
                            string title = reader.GetString(reader.GetOrdinal("c_title"));
                            Console.WriteLine($"{userId} {relatedId} {title} ////////////////////////");

                            return (userId, relatedId, title);
                        }
                    }
                }

                // Return default values if no task was found and deleted
                return (-1, -1, "Not Found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting task: {ex.Message}");
                return (-1, -1, "Error");
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }


        #endregion

        #region UpdateTask
        public async Task<int> UpdateTask(TaskModel task)
        {
            var query = @"UPDATE t_tasks 
                  SET c_title = @Title, 
                      c_description = @Description, 
                      c_status = @Status::task_status, 
                      c_priority = @Priority, 
                      c_due_date = @DueDate, 
                      c_updated_at = @UpdatedAt, 
                      c_assigned_to = @AssignedTo
                  WHERE c_task_id = @TaskId RETURNING c_task_id";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@TaskId", task.c_task_id);
                    cmd.Parameters.AddWithValue("@Title", task.c_title);
                    cmd.Parameters.AddWithValue("@Description", task.c_description);

                    // Ensure status is correctly handled (PostgreSQL Enum)
                    cmd.Parameters.AddWithValue("@Status", task.c_status.ToString());

                    cmd.Parameters.AddWithValue("@Priority", (object?)task.c_priority ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DueDate", (object?)task.c_due_date ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow); // Updating the timestamp
                    cmd.Parameters.AddWithValue("@AssignedTo", (object?)task.c_assigned_to ?? DBNull.Value);

                    int rowsAffected = (int)await cmd.ExecuteScalarAsync();
                    return rowsAffected; // Returns true if a row was updated
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating task: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        #endregion


        #region get task by user and admin



        public async Task<List<TaskModel>> GetTasksByCreatorAndAssignee(int createdBy, int assignedTo)
        {

            Console.WriteLine($"admin id {createdBy} user id {assignedTo}");
            var query = @"SELECT c_task_id, c_title, c_description, c_status, c_priority, 
                         c_due_date, c_created_at, c_updated_at, c_created_by, c_assigned_to
                  FROM t_tasks 
                  WHERE c_created_by = @CreatedBy AND c_assigned_to = @AssignedTo";

            var tasks = new List<TaskModel>();

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                    cmd.Parameters.AddWithValue("@AssignedTo", assignedTo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var task = new TaskModel
                            {
                                c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                                c_title = reader.GetString(reader.GetOrdinal("c_title")),
                                c_description = reader.GetString(reader.GetOrdinal("c_description")),
                                c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                                c_priority = reader.IsDBNull(reader.GetOrdinal("c_priority")) ? null : reader.GetInt32(reader.GetOrdinal("c_priority")),
                                c_due_date = reader.IsDBNull(reader.GetOrdinal("c_due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                                c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                                c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                                c_created_by = reader.GetInt32(reader.GetOrdinal("c_created_by")),
                                c_assigned_to = reader.IsDBNull(reader.GetOrdinal("c_assigned_to")) ? null : reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                            };

                            tasks.Add(task);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tasks: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return tasks;
        }

        #endregion




    }
}