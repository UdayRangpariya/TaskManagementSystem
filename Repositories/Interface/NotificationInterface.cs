using System;
using System.Collections.Generic;
using System.Linq;
using Repositories.Model.AdminModels;
using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface NotificationInterface
    {
        public Task<int> GetUnreadNotificationCount(int userId);
        public Task<bool> MarkAllNotificationsAsRead(int userId);
        public Task<bool> MarkNotificationAsRead(int notificationId);
        public Task<List<NotificationModel>> GetUserNotifications(int userId, int limit = 20);
        public Task<bool> CreateNotification(NotificationModel notification);
    }
}