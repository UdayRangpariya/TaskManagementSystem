using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repositories.Model.AdminModels;
using StackExchange.Redis;

namespace API.Services
{
    public class RedisService : IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisService(IConfiguration configuration)
        {
            try
            {
                string redisConnection = configuration["Redis:ConnectionString"] ?? "localhost";
                _redis = ConnectionMultiplexer.Connect(redisConnection);
                _db = _redis.GetDatabase();
                Console.WriteLine("Redis connection established");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Redis: {ex.Message}");
                throw;
            }
        }

     

        public async Task CacheNotification(NotificationModel notification)
        {
            try
            {
                // Cache individual notification
                string notificationKey = $"notification:{notification.c_notification_id}";
                await _db.StringSetAsync(
                    notificationKey,
                    JsonSerializer.Serialize(notification),
                    expiry: TimeSpan.FromDays(7));

                // Add to recipient's notification list (c_related_user_id)
                string userNotificationsKey = $"user:{notification.c_related_user_id}:notifications";
                await _db.ListLeftPushAsync(userNotificationsKey, notification.c_notification_id);

                // Update unread count only if unread
                if (!notification.c_is_read)
                {
                    string unreadCountKey = $"user:{notification.c_related_user_id}:unread_count";
                    await _db.StringIncrementAsync(unreadCountKey);
                }

                // Set expiry on user keys
                await _db.KeyExpireAsync(userNotificationsKey, TimeSpan.FromDays(30));
                await _db.KeyExpireAsync($"user:{notification.c_related_user_id}:unread_count", TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error caching notification in Redis: {ex.Message}");
            }
        }

        

        public async Task<int> GetUnreadNotificationCount(int userId)
        {
            try
            {
                if (_db == null)
                {
                    Console.WriteLine("Redis database instance is null");
                    return 0;
                }

                string unreadCountKey = $"user:{userId}:unread_count";
                var value = await _db.StringGetAsync(unreadCountKey);

                if (value.HasValue && (int)value > 0)
                {
                    Console.WriteLine($"Unread count from key {unreadCountKey}: {(int)value}");
                    return (int)value;
                }

                // If count is 0 or missing, calculate from notifications
                string userNotificationsKey = $"user:{userId}:notifications";
                var notificationIds = await _db.ListRangeAsync(userNotificationsKey);
                if (notificationIds == null || notificationIds.Length == 0)
                {
                    Console.WriteLine($"No notifications found for key {userNotificationsKey}");
                    return 0;
                }

                int unreadCount = 0;
                foreach (var id in notificationIds)
                {
                    string notificationKey = $"notification:{id}";
                    var notificationJson = await _db.StringGetAsync(notificationKey);
                    if (notificationJson.HasValue)
                    {
                        var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson.ToString());
                        if (notification != null && !notification.c_is_read)
                        {
                            unreadCount++;
                        }
                    }
                }

                if (unreadCount > 0)
                {
                    await _db.StringSetAsync(unreadCountKey, unreadCount, expiry: TimeSpan.FromDays(30));
                    Console.WriteLine($"Set unread count for {unreadCountKey} to {unreadCount}");
                }

                return unreadCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count from Redis: {ex.Message}");
                return 0;
            }
        }


        public async Task<List<NotificationModel>> GetNotifications(int userId, int limit = 20)
        {
            try
            {
                string userNotificationsKey = $"user:{userId}:notifications";
                var notificationIds = await _db.ListRangeAsync(userNotificationsKey, 0, limit - 1);

                var notifications = new List<NotificationModel>();
                foreach (var notificationId in notificationIds)
                {
                    string notificationKey = $"notification:{notificationId}";
                    var notificationJson = await _db.StringGetAsync(notificationKey);

                    if (notificationJson.HasValue)
                    {
                        var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson.ToString());
                        notifications.Add(notification);
                    }
                }

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications from Redis: {ex.Message}");
                return new List<NotificationModel>();
            }
        }



        public async Task<List<NotificationModel>> GetUserNotifications(int userId)
        {
            try
            {
                if (_db == null)
                {
                    Console.WriteLine("Redis database instance is null");
                    return new List<NotificationModel>();
                }

                string userNotificationsKey = $"user:{userId}:notifications";
                var notificationIds = await _db.ListRangeAsync(userNotificationsKey);

                if (notificationIds == null || notificationIds.Length == 0)
                {
                    Console.WriteLine($"No notifications found for user {userId} in Redis");
                    return new List<NotificationModel>();
                }

                var notifications = new List<NotificationModel>();
                foreach (var id in notificationIds)
                {
                    string notificationKey = $"notification:{id}";
                    var notificationJson = await _db.StringGetAsync(notificationKey);
                    if (notificationJson.HasValue)
                    {
                        var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson.ToString());
                        if (notification != null)
                        {
                            notifications.Add(notification);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Notification {id} not found in Redis");
                    }
                }

                Console.WriteLine($"Fetched {notifications.Count} notifications for user {userId} from Redis");
                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notifications for user {userId} from Redis: {ex.Message}");
                return new List<NotificationModel>();
            }
        }

         public async Task<bool> MarkNotificationAsRead(int userId, int notificationId)
    {
        try
        {
            if (_db == null)
            {
                Console.WriteLine("Redis database instance is null");
                return false;
            }

            string notificationKey = $"notification:{notificationId}";
            var notificationJson = await _db.StringGetAsync(notificationKey);

            if (!notificationJson.HasValue)
            {
                Console.WriteLine($"Notification {notificationId} not found in Redis");
                return false;
            }

            var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson.ToString());
            if (notification == null || notification.c_related_user_id != userId)
            {
                Console.WriteLine($"Notification {notificationId} does not belong to user {userId}");
                return false;
            }

            // Mark as read
            notification.c_is_read = true;
            await _db.StringSetAsync(notificationKey, JsonSerializer.Serialize(notification), expiry: TimeSpan.FromDays(7));

            // Remove from user's notification list
            string userNotificationsKey = $"user:{userId}:notifications";
            await _db.ListRemoveAsync(userNotificationsKey, notificationId.ToString());

            // Decrement unread count if it was previously unread
            if (!notification.c_is_read) // Note: This check is redundant since we just set it to true, but keeping for clarity
            {
                string unreadCountKey = $"user:{userId}:unread_count";
                var unreadCount = await _db.StringGetAsync(unreadCountKey);
                if (unreadCount.HasValue && (int)unreadCount > 0)
                {
                    await _db.StringDecrementAsync(unreadCountKey);
                }
            }

            Console.WriteLine($"Notification {notificationId} marked as read and removed for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking notification {notificationId} as read: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MarkAllNotificationsAsRead(int userId)
    {
        try
        {
            if (_db == null)
            {
                Console.WriteLine("Redis database instance is null");
                return false;
            }

            string userNotificationsKey = $"user:{userId}:notifications";
            var notificationIds = await _db.ListRangeAsync(userNotificationsKey);

            if (notificationIds == null || notificationIds.Length == 0)
            {
                Console.WriteLine($"No notifications to mark as read for user {userId}");
                return true; // Success, nothing to do
            }

            foreach (var id in notificationIds)
            {
                string notificationKey = $"notification:{id}";
                var notificationJson = await _db.StringGetAsync(notificationKey);

                if (notificationJson.HasValue)
                {
                    var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson.ToString());
                    if (notification != null && !notification.c_is_read)
                    {
                        notification.c_is_read = true;
                        await _db.StringSetAsync(notificationKey, JsonSerializer.Serialize(notification), expiry: TimeSpan.FromDays(7));
                    }
                }
            }

            // Clear the user's notification list
            await _db.KeyDeleteAsync(userNotificationsKey);

            // Reset unread count
            string unreadCountKey = $"user:{userId}:unread_count";
            await _db.StringSetAsync(unreadCountKey, 0, expiry: TimeSpan.FromDays(30));

            Console.WriteLine($"All notifications marked as read and removed for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking all notifications as read for user {userId}: {ex.Message}");
            return false;
        }
    }





    public async Task<bool> DeleteNotification(int userId, int notificationId)
    {
        try
        {
            if (_db == null)
            {
                Console.WriteLine("Redis database instance is null");
                return false;
            }

            string notificationKey = $"notification:{notificationId}";
            var notificationJson = await _db.StringGetAsync(notificationKey);

            if (!notificationJson.HasValue)
            {
                Console.WriteLine($"Notification {notificationId} not found in Redis");
                return false; // Notification already deleted or never cached
            }

            var notification = JsonSerializer.Deserialize<NotificationModel>(notificationJson);
            if (notification.c_related_user_id != userId)
            {
                Console.WriteLine($"Notification {notificationId} does not belong to user {userId}");
                return false;
            }

            // Delete the individual notification
            await _db.KeyDeleteAsync(notificationKey);

            // Remove from user's notification list
            string userNotificationsKey = $"user:{userId}:notifications";
            await _db.ListRemoveAsync(userNotificationsKey, notificationId.ToString());

            // Decrement unread count if it was unread
            if (!notification.c_is_read)
            {
                string unreadCountKey = $"user:{userId}:unread_count";
                var unreadCount = await _db.StringGetAsync(unreadCountKey);
                if (unreadCount.HasValue && (int)unreadCount > 0)
                {
                    await _db.StringDecrementAsync(unreadCountKey);
                }
            }

            Console.WriteLine($"Notification {notificationId} deleted from Redis for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting notification {notificationId} from Redis: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAllNotifications(int userId)
    {
        try
        {
            if (_db == null)
            {
                Console.WriteLine("Redis database instance is null");
                return false;
            }

            string userNotificationsKey = $"user:{userId}:notifications";
            var notificationIds = await _db.ListRangeAsync(userNotificationsKey);

            if (notificationIds.Length == 0)
            {
                Console.WriteLine($"No notifications to delete for user {userId}");
                return true; // Nothing to delete, still a success
            }

            // Delete each individual notification
            foreach (var id in notificationIds)
            {
                string notificationKey = $"notification:{id}";
                await _db.KeyDeleteAsync(notificationKey);
            }

            // Clear the user's notification list
            await _db.KeyDeleteAsync(userNotificationsKey);

            // Reset unread count
            string unreadCountKey = $"user:{userId}:unread_count";
            await _db.KeyDeleteAsync(unreadCountKey); // Or set to 0 with StringSetAsync

            Console.WriteLine($"All notifications deleted from Redis for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting all notifications for user {userId} from Redis: {ex.Message}");
            return false;
        }
    }

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}