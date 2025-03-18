using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Model.AdminModels;
using Repositories.Interface;



namespace Repositories.Implementation.UserRepo
{
    public class UserRepo : UserInterface
    {

        private readonly NpgsqlConnection _conn;

        public UserRepo(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task<IEnumerable<TaskModel>> GetAllTasks(int id )
        {
            var tasks = new List<TaskModel>();
      
            try
            {
                await _conn.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_tasks where c_assigned_to = @userId",
                    _conn);

                cmd.Parameters.AddWithValue("@userId", id);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tasks.Add(new TaskModel
                    {
                        c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                        c_title = reader.GetString(reader.GetOrdinal("c_title")),
                        c_description = reader.GetString(reader.GetOrdinal("c_description")),
                        c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                        c_priority = reader.GetInt32(reader.GetOrdinal("c_priority")),
                        c_due_date = reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                        c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                        c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                        c_created_by = reader.GetInt32(reader.GetOrdinal("c_created_by")),
                        c_assigned_to = reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                    });
                }
                return tasks;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }
        #region GetTaskbyid

        public async Task<TaskModel> GetTaskById(int taskId)
        {
            try
            {
                await _conn.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_tasks WHERE c_task_id = @taskId",
                    _conn);

                cmd.Parameters.AddWithValue("@taskId", taskId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new TaskModel
                    {
                        c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                        c_title = reader.GetString(reader.GetOrdinal("c_title")),
                        c_description = reader.GetString(reader.GetOrdinal("c_description")),
                        c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                        c_priority = reader.GetInt32(reader.GetOrdinal("c_priority")),
                        c_due_date = reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                        c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                        c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                        c_created_by = reader.GetInt32(reader.GetOrdinal("c_created_by")),
                        c_assigned_to = reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                    };
                }
                return null;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }
        #endregion


        public async Task<TaskModel> AddTask(TaskModel task)
        {
            try
            {
                await _conn.OpenAsync();
                using var cmd = new NpgsqlCommand(
                      @"INSERT INTO t_tasks (c_title, c_description, c_status, c_priority, 
            c_due_date, c_created_by, c_assigned_to) 
            VALUES (@title, @description, @status::task_status, @priority, @dueDate, 
            @createdBy, @assignedTo) RETURNING *",
                    _conn);

                cmd.Parameters.AddWithValue("@title", task.c_title);
                cmd.Parameters.AddWithValue("@description", (object)task.c_description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", task.c_status.ToString());
                cmd.Parameters.AddWithValue("@priority", (object)task.c_priority ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dueDate", (object)task.c_due_date ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@createdBy", (object)task.c_created_by ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@assignedTo", (object)task.c_assigned_to ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new TaskModel
                    {
                        c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
                        c_title = reader.GetString(reader.GetOrdinal("c_title")),
                        c_description = reader.IsDBNull(reader.GetOrdinal("c_description"))
                            ? null : reader.GetString(reader.GetOrdinal("c_description")),
                        c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
                        c_priority = reader.IsDBNull(reader.GetOrdinal("c_priority"))
                            ? null : reader.GetInt32(reader.GetOrdinal("c_priority")),
                        c_due_date = reader.IsDBNull(reader.GetOrdinal("c_due_date"))
                            ? null : reader.GetDateTime(reader.GetOrdinal("c_due_date")),
                        c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
                        c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
                        c_created_by = reader.IsDBNull(reader.GetOrdinal("c_created_by"))
                            ? null : reader.GetInt32(reader.GetOrdinal("c_created_by")),
                        c_assigned_to = reader.IsDBNull(reader.GetOrdinal("c_assigned_to"))
                            ? null : reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
                    };
                }
                return null;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }


        public async Task<bool> DeleteTask(int taskId)
        {
            try
            {
                await _conn.OpenAsync();

                // First check if task was created by admin
                using var checkCmd = new NpgsqlCommand(
                    @"SELECT u.c_role 
            FROM t_tasks t 
            JOIN t_users u ON t.c_created_by = u.c_user_id 
            WHERE t.c_task_id = @taskId",
                    _conn);

                checkCmd.Parameters.AddWithValue("@taskId", taskId);

                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string creatorRole = reader.GetString(0);
                    if (creatorRole.ToLower() == "admin")
                    {
                        return false; // Cannot delete admin-created tasks
                    }
                }

                await reader.CloseAsync();

                // If task was not created by admin, proceed with deletion
                using var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM t_tasks WHERE c_task_id = @taskId",
                    _conn);

                deleteCmd.Parameters.AddWithValue("@taskId", taskId);

                int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }


        public async Task<TaskModel> UpdateTask(TaskModel task)
        {
            try
            {
                await _conn.OpenAsync();

                // Step 1: Get the creator's role for the task
                using var checkCmd = new NpgsqlCommand(
                    @"SELECT u.c_role
              FROM t_tasks t 
              LEFT JOIN t_users u ON t.c_created_by = u.c_user_id 
              WHERE t.c_task_id = @taskId",
                    _conn);

                checkCmd.Parameters.AddWithValue("@taskId", task.c_task_id);

                using var reader = await checkCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string creatorRole = reader.GetString(0); // Creator's role

                    // Step 2: Close the reader to proceed to the update
                    await reader.CloseAsync();

                    // Step 3: Decide the update behavior based on the role
                    if (creatorRole.ToLower() == "admin")
                    {
                        // For admin: only allow status update
                        using var updateCmd = new NpgsqlCommand(
                            @"UPDATE t_tasks SET 
                      c_status = @status::task_status,  -- Cast status to task_status enum
                      c_updated_at = CURRENT_TIMESTAMP
                      WHERE c_task_id = @taskId 
                      RETURNING *",
                            _conn);

                        updateCmd.Parameters.AddWithValue("@taskId", task.c_task_id);
                        updateCmd.Parameters.AddWithValue("@status", task.c_status.ToString()); // Ensure it's a string

                        using var updateReader = await updateCmd.ExecuteReaderAsync();
                        if (await updateReader.ReadAsync())
                        {

                            Console.WriteLine($" assigne to id form the repo{updateReader.GetInt32(updateReader.GetOrdinal("c_assigned_to"))} created by id form the repo {updateReader.GetInt32(updateReader.GetOrdinal("c_created_by"))}");
                            return new TaskModel
                            {


                                c_task_id = updateReader.GetInt32(updateReader.GetOrdinal("c_task_id")),
                                c_title = updateReader.GetString(updateReader.GetOrdinal("c_title")),
                                c_description = updateReader.IsDBNull(updateReader.GetOrdinal("c_description"))
                                    ? null : updateReader.GetString(updateReader.GetOrdinal("c_description")),
                                c_status = Enum.Parse<task_status>(updateReader.GetString(updateReader.GetOrdinal("c_status"))),
                                c_priority = updateReader.IsDBNull(updateReader.GetOrdinal("c_priority"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_priority")),
                                c_due_date = updateReader.IsDBNull(updateReader.GetOrdinal("c_due_date"))
                                    ? null : updateReader.GetDateTime(updateReader.GetOrdinal("c_due_date")),
                                c_created_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_created_at")),
                                c_updated_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_updated_at")),
                                c_created_by = updateReader.IsDBNull(updateReader.GetOrdinal("c_created_by"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_created_by")),
                                c_assigned_to = updateReader.IsDBNull(updateReader.GetOrdinal("c_assigned_to"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_assigned_to"))
                            };
                        }
                    }
                    else
                    {

                        using var updateCmd = new NpgsqlCommand(
                            @"UPDATE t_tasks SET 
                      c_title = @title, 
                      c_description = @description,
                      c_status = @status::task_status,  -- Cast status to task_status enum
                      c_priority = @priority,
                      c_due_date = @dueDate,
                      c_assigned_to = @assignedTo,
                      c_updated_at = CURRENT_TIMESTAMP
                      WHERE c_task_id = @taskId 
                      RETURNING *",
                            _conn);

                        updateCmd.Parameters.AddWithValue("@taskId", task.c_task_id);
                        updateCmd.Parameters.AddWithValue("@title", task.c_title);
                        updateCmd.Parameters.AddWithValue("@description", (object)task.c_description ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@status", task.c_status.ToString()); // Ensure it's a string
                        updateCmd.Parameters.AddWithValue("@priority", (object)task.c_priority ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@dueDate", (object)task.c_due_date ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@assignedTo", (object)task.c_assigned_to ?? DBNull.Value);

                        using var updateReader = await updateCmd.ExecuteReaderAsync();
                        if (await updateReader.ReadAsync())
                        {
                            return new TaskModel
                            {
                                c_task_id = updateReader.GetInt32(updateReader.GetOrdinal("c_task_id")),
                                c_title = updateReader.GetString(updateReader.GetOrdinal("c_title")),
                                c_description = updateReader.IsDBNull(updateReader.GetOrdinal("c_description"))
                                    ? null : updateReader.GetString(updateReader.GetOrdinal("c_description")),
                                c_status = Enum.Parse<task_status>(updateReader.GetString(updateReader.GetOrdinal("c_status"))),
                                c_priority = updateReader.IsDBNull(updateReader.GetOrdinal("c_priority"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_priority")),
                                c_due_date = updateReader.IsDBNull(updateReader.GetOrdinal("c_due_date"))
                                    ? null : updateReader.GetDateTime(updateReader.GetOrdinal("c_due_date")),
                                c_created_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_created_at")),
                                c_updated_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_updated_at")),
                                c_created_by = updateReader.IsDBNull(updateReader.GetOrdinal("c_created_by"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_created_by")),
                                c_assigned_to = updateReader.IsDBNull(updateReader.GetOrdinal("c_assigned_to"))
                                    ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_assigned_to"))
                            };
                        }
                    }
                }
                return null; // Return null if task was not found
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }




    }
}