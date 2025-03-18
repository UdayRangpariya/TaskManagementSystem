using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Repositories.Model
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
    public class User
    {



        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public user_role Role { get; set; } = user_role.user;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

     

    }
}