using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using API.Services;
using Repositories.Interface;
using Repositories.Model.AdminModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;



namespace API.Controller.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly AdminInterface _repo;
        private readonly NotificationInterface _notificationRepo;
        private readonly NotificationService _notificationService;

        public AdminApiController(
            AdminInterface repo,
            NotificationService notificationService,
            NotificationInterface notificationRepo)  // Add this parameter
        {
            _repo = repo;
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;  // Initialize _notificationRepo
        }
        #region User Endpoints

        [HttpGet]
        [Route("users")]
        public async Task<ActionResult<List<UserModel>>> GetAllUsers()
        {
            try
            {
                var users = await _repo.GetAllUsers();
                if (!users.Any())
                    return NoContent();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while fetching users", error = ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserModel>> GetUserById(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid user ID" });

                var user = await _repo.GetUserById(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while fetching user", error = ex.Message });
            }
        }

        #endregion

        #region Task Endpoints

        [HttpGet("tasks")]
        public async Task<ActionResult<List<TaskModel>>> GetAllTasks()
        {
            try
            {
                var tasks = await _repo.GetAllTasks();
                if (!tasks.Any())
                    return NoContent();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while fetching tasks", error = ex.Message });
            }
        }

        [HttpGet("tasks/user/{userId}")]
        public async Task<ActionResult<List<TaskModel>>> GetTasksByUserId(int userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { message = "Invalid user ID" });

                var tasks = await _repo.GetTasksByUserId(userId);
                if (!tasks.Any())
                    return NotFound(new { message = $"No tasks found for user ID {userId}" });

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while fetching user tasks", error = ex.Message });
            }
        }

        #endregion
        #region AddTask

        [HttpPost("Addtasks")]
        public async Task<ActionResult> AddTask([FromBody] TaskModel newTask)
        {
            try
            {
                if (newTask == null || string.IsNullOrEmpty(newTask.c_title) || string.IsNullOrEmpty(newTask.c_description))
                    return BadRequest(new { message = "Task data, title, and description are required" });

                int adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 1; // Default to 1 for testing
                newTask.c_created_by = adminId;

                int newTaskId = await _repo.AssignTask(newTask);
                if (newTaskId <= 0)
                    return StatusCode(500, new { message = "Failed to add task. Please try again." });

                Console.WriteLine($"Task added successfully with ID: {newTaskId}");

                await _notificationService.SendTaskNotification(
                    senderId: adminId,
                    recipientId: newTask.c_assigned_to ?? 0,
                    taskId: newTaskId,
                    taskTitle: newTask.c_title,
                    type: NotificationType.task_created
                );

                return Ok(new { message = "Task added successfully", taskId = newTaskId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while adding task", error = ex.Message });
            }
        }

                #endregion

                #region UpdateTask

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskModel task)
        {
            try
            {
                if (task == null || id != task.c_task_id)
                    return BadRequest("Invalid task data.");

                int adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idClaim) ? idClaim : 1; // Default to 1 for testing

                int updated = await _repo.UpdateTask(task);
                if (updated < 0)
                    return NotFound("Task not found or update failed.");

                await _notificationService.SendTaskNotification(
                    senderId: adminId,
                    recipientId: task.c_assigned_to ?? 0,
                    taskId: task.c_task_id,
                    taskTitle: task.c_title,
                    type: NotificationType.task_updated
                );

                return Ok("Task updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while updating task", error = ex.Message });
            }
        }

#endregion
        #region Delete Task

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                int adminId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idClaim) ? idClaim : 1; // Default to 1 for testing

                var deletedTask = await _repo.DeleteTaskAsync(id);
                if (deletedTask.c_created_by == -1 && deletedTask.c_assigned_to == -1 && deletedTask.c_title == "Not Found")
                    return NotFound("Task not found or delete failed.");

                Console.WriteLine($"Task deleted: {deletedTask.c_title} by admin {adminId}");

                await _notificationService.SendTaskNotification(
                    senderId: adminId,
                    recipientId: deletedTask.c_assigned_to,
                    taskId: id,
                    taskTitle: deletedTask.c_title,
                    type: NotificationType.task_deleted
                );

                return Ok(new
                {
                    Message = "Task deleted successfully.",
                    DeletedTask = deletedTask
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while deleting task", error = ex.Message });
            }
        }

        #endregion


        #region get Task by user and admin

        [HttpGet]
        public async Task<ActionResult<List<TaskModel>>> GetTasksByCreatorAndAssignee(int createdBy, int assignedTo)
        {
            try
            {
                if (createdBy <= 0 || assignedTo <= 0)
                    return BadRequest(new { message = "Invalid user ID" });

                var tasks = await _repo.GetTasksByCreatorAndAssignee(createdBy, assignedTo);
                if (!tasks.Any())
                    return NotFound(new { message = $"No tasks found for user ID {createdBy}" });

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while fetching user tasks", error = ex.Message });
            }
        }

        #endregion



        #region Get Notifications

        [HttpGet("notifications")]
        public async Task<ActionResult> GetNotifications([FromQuery] int userId, [FromQuery] int limit = 20)
        {
            try
            {
                if (_notificationRepo == null)
                    return StatusCode(500, new { message = "Notification repository is not available" });

                var notifications = await _notificationRepo.GetUserNotifications(userId, limit);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting notifications", error = ex.Message });
            }
        }

        [HttpGet("notifications/unread-count")]
        public async Task<ActionResult> GetUnreadNotificationCount([FromQuery] int userId)
        {
            try
            {
                if (_notificationRepo == null)
                    return StatusCode(500, new { message = "Notification repository is not available" });

                int count = await _notificationRepo.GetUnreadNotificationCount(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting unread notification count", error = ex.Message });
            }
        }

        #endregion
        // [HttpPut("notifications/{notificationId}/read")]
        // public async Task<ActionResult> MarkNotificationAsRead(int notificationId)
        // {
        //     try
        //     {
        //         // Hardcoding userId for now; in production, get from auth
        //         int userId = 2; // Replace with actual user ID from authentication
        //         bool result = await _notificationService.MarkNotificationAsRead(userId, notificationId);
        //         return Ok(new { success = result });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { message = "Error marking notification as read", error = ex.Message });
        //     }
        // }

        // [HttpPut("notifications/read-all")]
        // public async Task<ActionResult> MarkAllNotificationsAsRead([FromQuery] int userId)
        // {
        //     userId = 2; // Hardcoding for now; replace with auth in production
        //     Console.WriteLine("user id = " + userId);
        //     try
        //     {
        //         bool result = await _notificationService.MarkAllNotificationsAsRead(userId);
        //         Console.WriteLine($"All notifications marked as read for user {userId} and result = {result}");
        //         return Ok(new { success = result });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new { message = "Error marking all notifications as read", error = ex.Message });
        //     }
        // }



        #region Get My Notifications

        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int userId = int.TryParse(userIdClaim, out int id) ? id : 1; // Fallback for testing

                var (success, notifications) = await _notificationService.GetUserNotifications(userId);
                if (!success)
                    return StatusCode(500, new { message = "Failed to fetch notifications" });

                return Ok(new { notifications });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching notifications", error = ex.Message });
            }
        }

        #endregion








    }
}