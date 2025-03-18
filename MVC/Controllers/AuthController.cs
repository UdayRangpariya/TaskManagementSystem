using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MVC.Services;
using Repositories.Model;

namespace MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiClientService _apiClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApiClientService apiClient, ILogger<AuthController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            // If user is already logged in, redirect to home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        
        [HttpPost]
        [Route("Auth/UploadProfilePicture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile ProfilePictureFile, string Email)
        {
            try
            {
                string profilePicturePath = "default.png";

                if (ProfilePictureFile != null && ProfilePictureFile.Length > 0)
                {
                    // Create uploads directory if it doesn't exist
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Get file extension
                    string fileExtension = Path.GetExtension(ProfilePictureFile.FileName).ToLowerInvariant();

                    // Check if it's a valid image extension
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new { message = "Invalid file type. Allowed types: .jpg, .jpeg, .png, .gif" });
                    }

                    // Limit file size to 5MB
                    if (ProfilePictureFile.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "File size exceeds 5MB limit" });
                    }

                    // Generate filename based on email address if provided, otherwise use timestamp
                    string fileName;
                    if (!string.IsNullOrEmpty(Email))
                    {
                        // Sanitize email for filename (replace invalid characters)
                        string sanitizedEmail = Email.Replace("@", "_at_").Replace(".", "_dot_");
                        fileName = $"{sanitizedEmail}{fileExtension}";
                    }
                    else
                    {
                        // Use timestamp if email not provided
                        fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_{Guid.NewGuid().ToString().Substring(0, 8)}{fileExtension}";
                    }

                    string filePath = Path.Combine(uploadsFolder, fileName);
                    profilePicturePath = $"/uploads/{fileName}";

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ProfilePictureFile.CopyToAsync(fileStream);
                    }
                }

                return Ok(new { profilePicturePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred while uploading the file: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}