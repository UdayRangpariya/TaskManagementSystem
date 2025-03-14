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

namespace API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuth _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuth authService, IConfiguration configuration)
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

                // Don't include sensitive information in the response
                return Ok(new
                {
                    message = "Registration successful",
                    user = new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role,
                        firstName = user.FirstName,
                        lastName = user.LastName
                        // Notice password is not included here
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim("Userid", user.UserId.ToString()),
                        new Claim("UserName", user.Username)
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                    var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddDays(1),
                        signingCredentials: signIn
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        user = new
                        {
                            userId = user.UserId,
                            username = user.Username,
                            email = user.Email,
                            role = user.Role,
                            firstName = user.FirstName,
                            lastName = user.LastName
                        },
                        token = new JwtSecurityTokenHandler().WriteToken(token)
                    });
                }
                
                return Unauthorized(new { message = "Invalid login attempt" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}