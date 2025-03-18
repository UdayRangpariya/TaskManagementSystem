using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Model.AdminModels
{

    public enum user_role
    {
        admin,
        user
    }

    public enum task_status
    {
        pending,
        in_progress,
        completed
    }

    public enum notification_type
    {
        task_created,
        task_updated,
        task_deleted,
        user_registered,
        message_received
    }
    public class UserModel
    {



        public int c_user_id { get; set; }
        public string c_username { get; set; }
        public string c_email { get; set; }
        public string c_password_hash { get; set; }
        public user_role c_role { get; set; } = user_role.user;
        public string c_first_name { get; set; }
        public string c_last_name { get; set; }
        public string c_profile_picture { get; set; }
        public DateTime c_created_at { get; set; } = DateTime.UtcNow;
        public DateTime? c_last_login { get; set; }
        public bool c_is_active { get; set; } = true;

     

    }
}