using APITest.Models;
using APITest.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace APITest.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    [ApiKeyAuthorize]
    [Authorize]
    public class TasksController : ControllerBase
    {
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        [EnableRateLimiting("TasksReadPolicy")]
        public IActionResult GetTasks()
        {
            var tasks = new[]
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Prepare sprint board",
                    Description = "Set up tasks for the upcoming sprint.",
                    AssignedTo = "manager",
                    IsCompleted = false
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Review onboarding checklist",
                    Description = "Confirm employee task visibility requirements.",
                    AssignedTo = "employee",
                    IsCompleted = false
                }
            };

            return Ok(tasks);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        [EnableRateLimiting("TasksWritePolicy")]
        public IActionResult CreateTask([FromBody] CreateTaskRequest request)
        {
            return Ok(new
            {
                message = "Task created.",
                task = new
                {
                    Id = 999,
                    request.Title,
                    request.Description,
                    request.AssignedTo,
                    IsCompleted = false
                }
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        [EnableRateLimiting("TasksWritePolicy")]
        public IActionResult UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            return Ok(new
            {
                message = $"Task with id {id} updated.",
                task = new
                {
                    Id = id,
                    request.Title,
                    request.Description,
                    request.AssignedTo,
                    request.IsCompleted
                }
            });
        }
    }
}
