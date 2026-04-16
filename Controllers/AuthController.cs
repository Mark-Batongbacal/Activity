using APITest.Models;
using APITest.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APITest.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [ApiKeyAuthorize]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel login)
        {
            var user = GetKnownUsers().FirstOrDefault(u =>
                u.Username.Equals(login.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == login.Password);

            if (user is null)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }

            var token = _jwtService.GenerateToken(user.Username, user.Role);

            return Ok(new
            {
                token,
                username = user.Username,
                role = user.Role,
                expiresInMinutes = 60
            });
        }

        private static IEnumerable<AppUser> GetKnownUsers()
        {
            return
            [
                new AppUser { Id = 1, Username = "admin", Password = "admin123", Role = "Admin" },
                new AppUser { Id = 2, Username = "manager", Password = "manager123", Role = "Manager" },
                new AppUser { Id = 3, Username = "employee", Password = "employee123", Role = "Employee" }
            ];
        }
    }
}
