using System.ComponentModel.DataAnnotations;
using Repositories.Model;

namespace Repositories.Model
{
    public class UserUpdate
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        [EmailAddress]
        public string Email { get; set; }
        
        public string ProfilePicture { get; set; }
        public user_role? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}