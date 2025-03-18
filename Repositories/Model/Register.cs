using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Repositories.Model
{
    public class Register
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        // Add JsonIgnore to prevent the password from appearing in responses
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePicture { get; set; }
    }
}