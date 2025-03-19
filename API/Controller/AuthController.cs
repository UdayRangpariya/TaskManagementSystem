
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories.Interface;
using Repositories.Model;
using MimeKit;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;

namespace API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthInterface _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthInterface authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _authService.RegisterUserAsync(model);

                if (user != null && user.UserId != 0)  // Assuming successful registration returns a valid user with ID
                {
                    try
                    {
                        await SendConfirmationEmail(user.Email, user.Username, model.Password);
                        return Ok(new
                        {
                            success = true,
                            message = "Registration successful and confirmation email sent",
                            user = new
                            {
                                userId = user.UserId,
                                username = user.Username,
                                email = user.Email,
                                role = user.Role,
                                firstName = user.FirstName,
                                lastName = user.LastName
                            }
                        });
                    }
                    catch (Exception emailEx)
                    {
                        // Still return success but notify about email failure
                        return Ok(new
                        {
                            success = true,
                            message = "Registration successful but failed to send confirmation email: " + emailEx.Message,
                            user = new
                            {
                                userId = user.UserId,
                                username = user.Username,
                                email = user.Email,
                                role = user.Role,
                                firstName = user.FirstName,
                                lastName = user.LastName
                            }
                        });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "User registration failed" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _authService.LoginUserAsync(model);

                if (user != null && user.UserId != 0)
                {
                    // Check if user is active
                    if (!user.IsActive)
                    {
                        return Unauthorized(new { message = "This account has been deactivated. Please contact an administrator." });
                    }

                    // Check if the requested role matches the user's actual role (when role was specified)
                    if (!string.IsNullOrEmpty(model.role) &&
                        !user.Role.ToString().Equals(model.role, StringComparison.OrdinalIgnoreCase))
                    {
                        return Unauthorized(new { message = "You don't have permission to access this area." });
                    }

                    // Create claims based on user information
                    var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                // Add role-specific claim
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

                    // Add name claims if available
                    if (!string.IsNullOrEmpty(user.FirstName))
                        claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));

                    if (!string.IsNullOrEmpty(user.LastName))
                        claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    // Set different token expiration based on role
                    var expiryDuration = user.Role.ToString().ToLower() == "admin" ? 7 : 1; // 7 days for admin, 1 day for regular users

                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddDays(expiryDuration),
                        signingCredentials: signIn
                    );

                    // Determine dashboard path based on role
                    string dashboardPath = user.Role.ToString().ToLower() == "admin"
                        ? "/admin/dashboard"
                        : "/user/dashboard";

                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        user = new
                        {
                            userId = user.UserId,
                            username = user.Username,
                            email = user.Email,
                            role = user.Role.ToString(),
                            firstName = user.FirstName,
                            lastName = user.LastName
                        },
                        dashboardPath = dashboardPath,
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        expiration = token.ValidTo
                    });
                }

                return Unauthorized(new { message = "Invalid login attempt" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Method to validate token and get current user info
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _authService.GetUserByIdAsync(int.Parse(userId));
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new
                {
                    userId = user.UserId,
                    username = user.Username,
                    email = user.Email,
                    role = user.Role,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    profilePicture = user.ProfilePicture,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task SendConfirmationEmail(string email, string username, string password)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Task Management Team", _configuration["EmailSettings:Username"]));
                message.To.Add(new MailboxAddress(username, email));
                message.Subject = "Your Task Management System Login Credentials";

                message.Body = new TextPart("html")
                {
                    Text = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <h2 style='color: #5cb85c;'>Welcome to Task Management System</h2>
                <p>Dear {username},</p>
                <p>Your user account has been created successfully. Below are your login details:</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Password:</strong> {password}</p>
                <p>You can log in to your account using the credentials provided above.</p>
                <p><a href='http://localhost:5205/login' style='color: #428bca;'>Log in to Your Account</a></p>
                <p>Best Regards,</p>
                <p><strong>Task Management Team</strong></p>
                <hr style='border-top: 1px solid #ddd;'>
                <p style='font-size: 0.9em; color: #555;'>This is an automated message, please do not reply directly to this email.</p>
            </div>
            "
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(
                        _configuration["EmailSettings:SmtpServer"],
                        int.Parse(_configuration["EmailSettings:SmtpPort"]),
                        SecureSocketOptions.StartTls);

                    await client.AuthenticateAsync(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]);

                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                // Log successful email
                Console.WriteLine($"Email sent successfully to: {email}");
            }
            catch (Exception ex)
            {
                // Log email error
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw; // Re-throw to be caught by the calling method
            }
        }

    }
}