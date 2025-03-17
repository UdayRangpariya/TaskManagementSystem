using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
namespace tmp

{
    public class TaskRepository : ITaskInterface
    {
        private readonly NpgsqlConnection _connection;

        public TaskRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<t_tasks> GetTaskById(int taskId)
        {
            try
            {
                await _connection.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_tasks WHERE c_task_id = @taskId",
                    _connection);

                cmd.Parameters.AddWithValue("@taskId", taskId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new t_tasks
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
                await _connection.CloseAsync();
            }
        }

        public async Task<IEnumerable<t_tasks>> GetAllTasks()
        {
            var tasks = new List<t_tasks>();
            try
            {
                await _connection.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM t_tasks where c_assigned_to = 2 ORDER BY c_created_at DESC ",
                    _connection);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tasks.Add(new t_tasks
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
                await _connection.CloseAsync();
            }
        }

        // public async Task<t_tasks> AddTask(t_tasks task)
        // {
        //     try
        //     {
        //         await _connection.OpenAsync();
        //         using var cmd = new NpgsqlCommand(
        //             @"INSERT INTO t_tasks (c_title, c_description, c_status, c_priority, 
        //             c_due_date, c_created_by, c_assigned_to) 
        //             VALUES (@title, @description, @status, @priority, @dueDate, 
        //             @createdBy, @assignedTo) RETURNING *",
        //             _connection);

        //         cmd.Parameters.AddWithValue("@title", task.c_title);
        //         cmd.Parameters.AddWithValue("@description", (object)task.c_description ?? DBNull.Value);
        //         // cmd.Parameters.AddWithValue("@status", task.c_status.ToString());
        //         cmd.Parameters.AddWithValue("@status", task.c_status);

        //         cmd.Parameters.AddWithValue("@priority", (object)task.c_priority ?? DBNull.Value);
        //         cmd.Parameters.AddWithValue("@dueDate", (object)task.c_due_date ?? DBNull.Value);
        //         cmd.Parameters.AddWithValue("@createdBy", (object)task.c_created_by ?? DBNull.Value);
        //         cmd.Parameters.AddWithValue("@assignedTo", (object)task.c_assigned_to ?? DBNull.Value);

        //         using var reader = await cmd.ExecuteReaderAsync();
        //         if (await reader.ReadAsync())
        //         {
        //             return new t_tasks
        //             {
        //                 c_task_id = reader.GetInt32(reader.GetOrdinal("c_task_id")),
        //                 c_title = reader.GetString(reader.GetOrdinal("c_title")),
        //                 c_description = reader.GetString(reader.GetOrdinal("c_description")),
        //                 c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status"))),
        //                 c_priority = reader.GetInt32(reader.GetOrdinal("c_priority")),
        //                 c_due_date = reader.GetDateTime(reader.GetOrdinal("c_due_date")),
        //                 c_created_at = reader.GetDateTime(reader.GetOrdinal("c_created_at")),
        //                 c_updated_at = reader.GetDateTime(reader.GetOrdinal("c_updated_at")),
        //                 c_created_by = reader.GetInt32(reader.GetOrdinal("c_created_by")),
        //                 c_assigned_to = reader.GetInt32(reader.GetOrdinal("c_assigned_to"))
        //             };
        //         }
        //         return null;
        //     }
        //     finally
        //     {
        //         await _connection.CloseAsync();
        //     }
        // }
        public async Task<t_tasks> AddTask(t_tasks task)
        {
            try
            {
                await _connection.OpenAsync();
                using var cmd = new NpgsqlCommand(
                      @"INSERT INTO t_tasks (c_title, c_description, c_status, c_priority, 
            c_due_date, c_created_by, c_assigned_to) 
            VALUES (@title, @description, @status::task_status, @priority, @dueDate, 
            @createdBy, @assignedTo) RETURNING *",
                    _connection);

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
                    return new t_tasks
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
                await _connection.CloseAsync();
            }
        }


        public async Task<bool> DeleteTask(int taskId)
        {
            try
            {
                await _connection.OpenAsync();

                // First check if task was created by admin
                using var checkCmd = new NpgsqlCommand(
                    @"SELECT u.c_role 
            FROM t_tasks t 
            JOIN t_users u ON t.c_created_by = u.c_user_id 
            WHERE t.c_task_id = @taskId",
                    _connection);

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
                    _connection);

                deleteCmd.Parameters.AddWithValue("@taskId", taskId);

                int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        // public async Task<t_tasks> UpdateTask(t_tasks task)
        // {
        //     try
        //     {
        //         await _connection.OpenAsync();

        //         using var checkCmd = new NpgsqlCommand(
        //             @"SELECT u.c_role
        //     FROM t_tasks t 
        //     LEFT JOIN t_users u ON t.c_created_by = u.c_user_id 
        //     WHERE t.c_task_id = @taskId",
        //             _connection);

        //         checkCmd.Parameters.AddWithValue("@taskId", task.c_task_id);

        //         using var reader = await checkCmd.ExecuteReaderAsync();
        //         Console.WriteLine("inside the update",reader);
        //         if (await reader.ReadAsync())
        //         {
        //             string creatorRole = reader.GetString(0);
        //             var originalTask = new t_tasks
        //             {
        //                 c_status = Enum.Parse<task_status>(reader.GetString(reader.GetOrdinal("c_status")))
        //             };

        //             await reader.CloseAsync();

        //             if (creatorRole.ToLower() == "admin")
        //             {
        //                 // For admin-created tasks, 
        //                 using var updateCmd = new NpgsqlCommand(
        //                     @"UPDATE t_tasks SET 
        //             c_status = @status::task_status,
        //             c_updated_at = CURRENT_TIMESTAMP
        //             WHERE c_task_id = @taskId 
        //             ",
        //                     _connection);

        //                 updateCmd.Parameters.AddWithValue("@taskId", task.c_task_id);
        //                 updateCmd.Parameters.AddWithValue("@status", task.c_status.ToString());

        //                 using var updateReader = await updateCmd.ExecuteReaderAsync();
        //                 if (await updateReader.ReadAsync())
        //                 {
        //                     return new t_tasks
        //                     {
        //                         c_task_id = updateReader.GetInt32(updateReader.GetOrdinal("c_task_id")),
        //                         c_title = updateReader.GetString(updateReader.GetOrdinal("c_title")),
        //                         c_description = updateReader.IsDBNull(updateReader.GetOrdinal("c_description"))
        //                             ? null : updateReader.GetString(updateReader.GetOrdinal("c_description")),
        //                         c_status = Enum.Parse<task_status>(updateReader.GetString(updateReader.GetOrdinal("c_status"))),
        //                         c_priority = updateReader.IsDBNull(updateReader.GetOrdinal("c_priority"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_priority")),
        //                         c_due_date = updateReader.IsDBNull(updateReader.GetOrdinal("c_due_date"))
        //                             ? null : updateReader.GetDateTime(updateReader.GetOrdinal("c_due_date")),
        //                         c_created_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_created_at")),
        //                         c_updated_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_updated_at")),
        //                         c_created_by = updateReader.IsDBNull(updateReader.GetOrdinal("c_created_by"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_created_by")),
        //                         c_assigned_to = updateReader.IsDBNull(updateReader.GetOrdinal("c_assigned_to"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_assigned_to"))
        //                     };
        //                 }
        //             }
        //             else
        //             {
        //                 // For user tasks, allow full updates
        //                 using var updateCmd = new NpgsqlCommand(
        //                     @"UPDATE t_tasks SET 
        //             c_title = @title, 
        //             c_description = @description,
        //             c_status = @status,
        //             c_priority = @priority,
        //             c_due_date = @dueDate,
        //             c_assigned_to = @assignedTo,
        //             c_updated_at = CURRENT_TIMESTAMP
        //             WHERE c_task_id = @taskId 
        //             RETURNING *",
        //                     _connection);

        //                 updateCmd.Parameters.AddWithValue("@taskId", task.c_task_id);
        //                 updateCmd.Parameters.AddWithValue("@title", task.c_title);
        //                 updateCmd.Parameters.AddWithValue("@description", (object)task.c_description ?? DBNull.Value);
        //                 updateCmd.Parameters.AddWithValue("@status", task.c_status.ToString());
        //                 updateCmd.Parameters.AddWithValue("@priority", (object)task.c_priority ?? DBNull.Value);
        //                 updateCmd.Parameters.AddWithValue("@dueDate", (object)task.c_due_date ?? DBNull.Value);
        //                 updateCmd.Parameters.AddWithValue("@assignedTo", (object)task.c_assigned_to ?? DBNull.Value);

        //                 using var updateReader = await updateCmd.ExecuteReaderAsync();
        //                 if (await updateReader.ReadAsync())
        //                 {
        //                     return new t_tasks
        //                     {
        //                         c_task_id = updateReader.GetInt32(updateReader.GetOrdinal("c_task_id")),
        //                         c_title = updateReader.GetString(updateReader.GetOrdinal("c_title")),
        //                         c_description = updateReader.IsDBNull(updateReader.GetOrdinal("c_description"))
        //                             ? null : updateReader.GetString(updateReader.GetOrdinal("c_description")),
        //                         c_status = Enum.Parse<task_status>(updateReader.GetString(updateReader.GetOrdinal("c_status"))),
        //                         c_priority = updateReader.IsDBNull(updateReader.GetOrdinal("c_priority"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_priority")),
        //                         c_due_date = updateReader.IsDBNull(updateReader.GetOrdinal("c_due_date"))
        //                             ? null : updateReader.GetDateTime(updateReader.GetOrdinal("c_due_date")),
        //                         c_created_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_created_at")),
        //                         c_updated_at = updateReader.GetDateTime(updateReader.GetOrdinal("c_updated_at")),
        //                         c_created_by = updateReader.IsDBNull(updateReader.GetOrdinal("c_created_by"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_created_by")),
        //                         c_assigned_to = updateReader.IsDBNull(updateReader.GetOrdinal("c_assigned_to"))
        //                             ? null : updateReader.GetInt32(updateReader.GetOrdinal("c_assigned_to"))
        //                     };
        //                 }
        //             }
        //         }
        //         return null;
        //     }
        //     finally
        //     {
        //         await _connection.CloseAsync();
        //     }
        // }

        public async Task<t_tasks> UpdateTask(t_tasks task)
{
    try
    {
        await _connection.OpenAsync();

        // Step 1: Get the creator's role for the task
        using var checkCmd = new NpgsqlCommand(
            @"SELECT u.c_role
              FROM t_tasks t 
              LEFT JOIN t_users u ON t.c_created_by = u.c_user_id 
              WHERE t.c_task_id = @taskId",
            _connection);

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
                    _connection);

                updateCmd.Parameters.AddWithValue("@taskId", task.c_task_id);
                updateCmd.Parameters.AddWithValue("@status", task.c_status.ToString()); // Ensure it's a string

                using var updateReader = await updateCmd.ExecuteReaderAsync();
                if (await updateReader.ReadAsync())
                {
                    return new t_tasks
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
                // For non-admin roles (e.g., regular user, manager): allow full updates
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
                    _connection);

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
                    return new t_tasks
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
        await _connection.CloseAsync();
    }
}

    }
}