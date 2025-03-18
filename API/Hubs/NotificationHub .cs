// using System;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.SignalR;
// using API.Services;
// namespace API.Hubs;
// public class NotificationHub : Hub
// {
//     private readonly NotificationService _notificationService;

//     public NotificationHub(NotificationService notificationService)
//     {
//         _notificationService = notificationService;
//     }

//     public async Task JoinUserGroup(string userId)
//     {
//         await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
//         Console.WriteLine($"User {userId} connected to notification hub");
//     }

//     public async Task MarkNotificationAsRead(string userId, int notificationId)
//     {
//         bool result = await _notificationService.MarkNotificationAsRead(int.Parse(userId), notificationId);
//         Console.WriteLine($"Marking reslt {result} as read for user {userId}");
//         Console.WriteLine($"Marking notification {notificationId} as read for user {userId}");
//         if (result)
//         {
//             Console.WriteLine($"Notification {notificationId} marked as read for user {userId}");
//         }
//         else
//         {
//             Console.WriteLine($"Failed to mark notification {notificationId} as read for user {userId}");
//         }
//     }

//     public async Task MarkAllNotificationsAsRead(string userId)
//     {
//         bool result = await _notificationService.MarkAllNotificationsAsRead(int.Parse(userId));
//         if (result)
//         {
//             Console.WriteLine($"All notifications marked as read for user {userId}");
//         }
//         else
//         {
//             Console.WriteLine($"Failed to mark all notifications as read for user {userId}");
//         }
//     }

//     public override async Task OnDisconnectedAsync(Exception exception)
//     {
//         Console.WriteLine($"User disconnected from notification hub");
//         await base.OnDisconnectedAsync(exception);
//     }
// }


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using API.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Hubs
{
    [Authorize] // Require authentication for all hub methods
    public class NotificationHub : Hub
    {
        private readonly NotificationService _notificationService;

        public NotificationHub(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task JoinUserGroup()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("User ID not found in token.");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            Console.WriteLine($"User {userId} connected to notification hub");
        }

        public async Task MarkNotificationAsRead(int notificationId)
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("User ID not found in token."));
            bool result = await _notificationService.MarkNotificationAsRead(userId, notificationId);
            Console.WriteLine($"Marking result {result} for notification {notificationId} by user {userId}");
            if (result)
            {
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
                Console.WriteLine($"Notification {notificationId} marked as read for user {userId}");
            }
            else
            {
                Console.WriteLine($"Failed to mark notification {notificationId} as read for user {userId}");
            }
        }

        public async Task MarkAllNotificationsAsRead()
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("User ID not found in token."));
            bool result = await _notificationService.MarkAllNotificationsAsRead(userId);
            Console.WriteLine($"Mark all result {result} for user {userId}");
            if (result)
            {
                await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
                Console.WriteLine($"All notifications marked as read for user {userId}");
            }
            else
            {
                Console.WriteLine($"Failed to mark all notifications as read for user {userId}");
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User {userId} connected to hub");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User {userId} disconnected from hub: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}