using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Model.AdminModels
{
    public enum NotificationType
    {
        task_created,
        task_updated,
        task_deleted,
        user_registered,
        message_received
    }
    public class NotificationModel
    {
        public int c_notification_id { get; set; }
        public NotificationType c_type { get; set; }
        public int c_user_id { get; set; }
        public int? c_related_user_id { get; set; }
        public int? c_recipient_id { get; set; }
        public int? c_task_id { get; set; }
        public string c_message { get; set; }
        public bool c_is_read { get; set; }
        public DateTime c_created_at { get; set; }

        // Helper properties for UI display
        public string FormattedDate => c_created_at.ToString("MMM dd, yyyy HH:mm");
        public string TypeIcon
        {
            get
            {
                return c_type switch
                {
                    NotificationType.task_created => "fa-plus-circle",
                    NotificationType.task_updated => "fa-edit",
                    NotificationType.task_deleted => "fa-trash",
                    NotificationType.user_registered => "fa-user-plus",
                    NotificationType.message_received => "fa-envelope",
                    _ => "fa-bell"
                };
            }
        }

    }
}