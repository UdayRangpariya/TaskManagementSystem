using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Repositories.Model.AdminModels;
using System.Threading.Tasks;
using Repositories.Interface;

namespace Repositories.Implementation.AdminRepo
{
    public class NotificationRepo : NotificationInterface
    {
        private readonly NpgsqlConnection _conn;
        public NotificationRepo(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public async Task<bool> CreateNotification(NotificationModel notification)
        {
            var query = @"INSERT INTO t_notifications 
                         (c_notification_id ,c_type, c_user_id, c_related_user_id, c_task_id, c_message, c_is_read, c_created_at) 
                         VALUES (@id,@Type::notification_type, @UserId, @RelatedUserId, @TaskId, @Message, @IsRead, @CreatedAt)";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {   cmd.Parameters.AddWithValue("@id", notification.c_notification_id);
                    cmd.Parameters.AddWithValue("@Type", notification.c_type.ToString().ToLower());
                    cmd.Parameters.AddWithValue("@UserId", notification.c_user_id);
                    cmd.Parameters.AddWithValue("@RelatedUserId", notification.c_related_user_id.HasValue ?
                        (object)notification.c_related_user_id : DBNull.Value);
                    cmd.Parameters.AddWithValue("@TaskId", notification.c_task_id.HasValue ?
                        (object)notification.c_task_id : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Message", notification.c_message);
                    cmd.Parameters.AddWithValue("@IsRead", notification.c_is_read);
                    cmd.Parameters.AddWithValue("@CreatedAt", notification.c_created_at);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating notification: {ex.Message}");
                return false;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<List<NotificationModel>> GetUserNotifications(int userId, int limit = 20)
        {
            var notifications = new List<NotificationModel>();
            var query = @"SELECT c_notification_id, c_type, c_user_id, c_related_user_id, 
                         c_task_id, c_message, c_is_read, c_created_at 
                         FROM t_notifications 
                         WHERE c_user_id = @UserId 
                         ORDER BY c_created_at DESC 
                         LIMIT @Limit";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Limit", limit);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new NotificationModel
                            {
                                c_notification_id = reader.GetInt32(0),
                                c_type = (NotificationType)Enum.Parse(typeof(NotificationType),
                                    reader.GetString(1), true),
                                c_user_id = reader.GetInt32(2),
                                c_related_user_id = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                                c_task_id = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                                c_message = reader.GetString(5),
                                c_is_read = reader.GetBoolean(6),
                                c_created_at = reader.GetDateTime(7)
                            });
                        }
                    }
                }
                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return notifications;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<bool> MarkNotificationAsRead(int notificationId)
        {

            Console.WriteLine($"notifiction id from the databawe {notificationId}");
            var query = "UPDATE t_notifications SET c_is_read = true WHERE c_notification_id = @NotificationId";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@NotificationId", notificationId);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                                   Console.WriteLine($"rows affertec {rowsAffected}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
                return false;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<bool> MarkAllNotificationsAsRead(int userId)
        {
          
            var query = "UPDATE t_notifications SET c_is_read = true WHERE c_user_id = @UserId AND c_is_read = false";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
     
                                        return true; // Return true even if no rows affected (all notifications might already be read)
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all notifications as read: {ex.Message}");
                return false;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> GetUnreadNotificationCount(int userId)
        {
            var query = "SELECT COUNT(*) FROM t_notifications WHERE c_user_id = @UserId AND c_is_read = false";

            try
            {
                await _conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(query, _conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread notification count: {ex.Message}");
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }



    }
}