

using System;
using System.Threading.Tasks;
using System.Text;
using API.Hubs;
using Repositories.Interface;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Repositories.Interface;
using Repositories.Model.AdminModels;

namespace API.Services
{
    public class NotificationService
    {
        private readonly NotificationInterface _notificationRepository;
        private readonly RabbitMQService _rabbitMQService;
        public readonly RedisService _redisService;
        private readonly IHubContext<NotificationHub> _hubContext;


        public NotificationService(
            NotificationInterface notificationRepository,
            RabbitMQService rabbitMQService,
            RedisService redisService,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _rabbitMQService = rabbitMQService;
            _redisService = redisService;
            _hubContext = hubContext;
        }



        public async Task<bool> SendTaskNotification(int senderId, int recipientId, int taskId, string taskTitle, NotificationType type)
        {
            try
            {
                string message;
                switch (type)
                {
                    case NotificationType.task_created:
                        message = $"New task '{taskTitle}' was assigned to you";
                        break;
                    case NotificationType.task_updated:
                        message = $"Task '{taskTitle}' was recently updated by {(senderId == recipientId ? "you" : $"user: {senderId}")}";
                        break;
                    case NotificationType.task_deleted:
                        message = $"Task '{taskTitle}' was deleted by {(senderId == recipientId ? "you" :  $"user: {senderId}")}";
                        break;
                    default:
                        message = $"Task '{taskTitle}' event occurred";
                        break;
                }

                var notification = new NotificationModel
                {
                    c_notification_id = new Random().Next(1000, 9999), // Replace with proper ID generation (e.g., DB-generated)
                    c_type = type,
                    c_user_id = senderId, // Who triggered the action (admin or user)
                    c_related_user_id = senderId == recipientId ? senderId : recipientId, // Context (e.g., assigned user or admin)
                    c_task_id = taskId,
                    c_message = message,
                    c_is_read = false,
                    c_created_at = DateTime.UtcNow
                };

                // Store in DB
                bool dbResult = await _notificationRepository.CreateNotification(notification);
                Console.WriteLine($"DB result: {dbResult}");

                // Publish to RabbitMQ (using updated RabbitMQService with recipientId)
                _rabbitMQService.PublishNotification(notification);

                // Cache in Redis
                await _redisService.CacheNotification(notification);

                // Send via SignalR
                await _hubContext.Clients.Group($"user_{recipientId}").SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        count = await _redisService.GetUnreadNotificationCount(recipientId),
                        notification = notification
                    });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending task notification: {ex.Message}");
                return false;
            }
        }





        public async Task<(bool Success, List<NotificationModel> Notifications)> GetUserNotifications(int userId)
        {
            try
            {
                var notifications = new List<NotificationModel>();
                var channel = _rabbitMQService.CreateConsumerChannel();
                var queueName = $"user_{userId}_notifications";

                channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(queueName, "task_notifications", $"user_{userId}", null);

                // Consume from RabbitMQ
                while (true)
                {
                    var result = channel.BasicGet(queueName, autoAck: false);
                    if (result == null) break;

                    var body = result.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var notification = JsonSerializer.Deserialize<NotificationModel>(message);

                    await _redisService.CacheNotification(notification);
                    notifications.Add(notification);
                    channel.BasicAck(result.DeliveryTag, multiple: false);
                }
                channel.Close();

                // Fetch all notifications from Redis (including those already cached)
                var redisNotifications = await _redisService.GetUserNotifications(userId);
                notifications.AddRange(redisNotifications.Where(rn => !notifications.Any(n => n.c_notification_id == rn.c_notification_id))); // Avoid duplicates

                if (notifications.Count > 0)
                {
                    await _hubContext.Clients.Group($"user_{userId}").SendAsync(
                        "ReceiveNotification",
                        new { notifications = notifications });
                }

                Console.WriteLine($"Fetched {notifications.Count} notifications for user {userId}");
                return (true, notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notifications for user {userId}: {ex.Message}");
                return (false, new List<NotificationModel>());
            }
        }








        public async Task<(bool Success, List<NotificationModel> Notifications)> GetNotifications(int userId)
        {
            try
            {
                var notifications = new List<NotificationModel>();
                var channel = _rabbitMQService.CreateConsumerChannel();
                var queueName = $"user_{userId}_notifications";

                // Ensure queue exists
                channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueBind(queueName, "task_notifications", $"user_{userId}", null);

                // Fetch all messages from the queue
                while (true)
                {
                    var result = channel.BasicGet(queueName, autoAck: false);
                    if (result == null) break; // No more messages

                    var body = result.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var notification = JsonSerializer.Deserialize<NotificationModel>(message);

                    // Store in Redis only when consumed
                    await _redisService.CacheNotification(notification);
                    notifications.Add(notification);

                    // Acknowledge the message to remove it from the queue
                    channel.BasicAck(result.DeliveryTag, multiple: false);
                }

                channel.Close();

                if (notifications.Count == 0)
                {
                    Console.WriteLine($"No new notifications found in queue for user {userId}");
                    // Return any existing notifications from Redis
                    var redisNotifications = await _redisService.GetNotifications(userId);
                    notifications.AddRange(redisNotifications);
                }

                if (notifications.Count > 0)
                {
                    // Push to SignalR
                    await _hubContext.Clients.Group($"user_{userId}").SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            count = await _redisService.GetUnreadNotificationCount(userId),
                            notifications = notifications
                        });
                }

                Console.WriteLine($"Fetched and cached {notifications.Count} notifications for user {userId}");
                return (true, notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notifications for user {userId}: {ex.Message}");
                return (false, new List<NotificationModel>());
            }
        }

        public async Task<bool> MarkNotificationAsRead(int userId, int notificationId)
        {
            try
            {
                // Update database
                bool dbResult = await _notificationRepository.MarkNotificationAsRead(notificationId);
                if (!dbResult)
                {
                    Console.WriteLine($"Failed to mark notification {notificationId} as read in database");
                    return false;
                }

                // Delete from Redis
                bool redisResult = await _redisService.DeleteNotification(userId, notificationId);
                if (!redisResult)
                {
                    Console.WriteLine($"Failed to delete notification {notificationId} from Redis, but DB updated");
                    // Proceed anyway since DB is source of truth
                }

                // Notify client via SignalR
                await _hubContext.Clients.Group($"user_{userId}").SendAsync(
                    "NotificationMarkedAsRead",
                    new
                    {
                        notificationId = notificationId,
                        unreadCount = await _redisService.GetUnreadNotificationCount(userId)
                    });

                Console.WriteLine($"Notification {notificationId} marked as read for user {userId}");
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
                // Update database
                bool dbResult = await _notificationRepository.MarkAllNotificationsAsRead(userId);
                if (!dbResult)
                {
                    Console.WriteLine($"Failed to mark all notifications as read in database for user {userId}");
                    return false;
                }

                // Delete all from Redis
                bool redisResult = await _redisService.DeleteAllNotifications(userId);
                if (!redisResult)
                {
                    Console.WriteLine($"Failed to delete all notifications from Redis for user {userId}, but DB updated");
                    // Proceed anyway since DB is source of truth
                }

                // Notify client via SignalR
                await _hubContext.Clients.Group($"user_{userId}").SendAsync(
                    "AllNotificationsMarkedAsRead",
                    new { unreadCount = 0 });

                Console.WriteLine($"All notifications marked as read and deleted for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all notifications as read for user {userId}: {ex.Message}");
                return false;
            }
        }



    }
}