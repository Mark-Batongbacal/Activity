using APITest.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APITest.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [ApiKeyAuthorize]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("AdminPolicy")]
    public class AdminController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = new[]
            {
                new { Id = 1, Username = "admin", Role = "Admin" },
                new { Id = 2, Username = "manager", Role = "Manager" },
                new { Id = 3, Username = "employee", Role = "Employee" }
            };

            return Ok(users);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteUser(int id)
        {
            return Ok(new { message = $"User with id {id} deleted." });
        }
    }
}
