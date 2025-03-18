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
using tmp;

namespace API.Controller.UserController
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "user")]
    public class UserController : ControllerBase
    {

        private readonly AdminInterface _repo;
        private readonly NotificationInterface _notificationRepo;
        private readonly NotificationService _notificationService;
        private readonly UserInterface _userRepo;
                private readonly ITaskInterface _taskrepo;
        private readonly ElasticsearchService _elasticService;



        public UserController(
            AdminInterface repo,
            NotificationService notificationService,
            NotificationInterface notificationRepo, UserInterface _userRepo,
            ITaskInterface taskrepo, ElasticsearchService elasticService)  // Add this parameter
        {
            _repo = repo;
            _taskrepo = taskrepo;
            _elasticService = elasticService;
            _notificationService = notificationService;
            _notificationRepo = notificationRepo;
            this._userRepo = _userRepo;
            // Initialize _notificationRepo
        }
        #region task by id
        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTaskById(int taskId)
        {
            var task = await _userRepo.GetTaskById(taskId);
            if (task == null)
            {
                return NotFound(new { success = false, message = "Task not found" });
            }

            return Ok(task);
        }

        #endregion

     [HttpGet("task/{name}/{userId}")]
        public async Task<IActionResult> SearchTaskByUser(string name, int userId)
        {
            var task = await _elasticService.SearchTasksByTitleAndUserIdAsync(name, userId);
            return task is null ? NotFound() : Ok(task);
        }



         [HttpPost("AddTask")]
        public async Task<IActionResult> Add([FromBody] TaskModel task)
        {
            if (task == null)
                return BadRequest(new { success = false, message = "Invalid task data" });

            if (string.IsNullOrWhiteSpace(task.c_title) || string.IsNullOrWhiteSpace(task.c_description))
                return BadRequest(new { success = false, message = "Title and description are required" });

            if (!task.c_assigned_to.HasValue || task.c_assigned_to <= 0)
                return BadRequest(new { success = false, message = "Task must be assigned to a user" });

            // Set default values
            task.c_created_at = DateTime.UtcNow;
            task.c_updated_at = DateTime.UtcNow;
            task.c_due_date ??= DateTime.UtcNow.AddDays(7);
            task.c_status = task_status.pending;

            // // Ensure creator is set
            // if (!task.c_created_by.HasValue || task.c_created_by <= 0)
            // {
            //     task.c_created_by = 1; // Default to admin if not set
            // }

            var addedTask = await _userRepo.AddTask(task);
            if (addedTask != null)
            {
                
              
                return Ok(new { 
                    success = true, 
                    message = "Task created successfully", 
                    task = addedTask 
                });
            }

            return BadRequest(new { success = false, message = "Failed to create task" });
        }

        [HttpPut("UpdateTask/{id}")]
        public async Task<IActionResult> Update([FromBody] TaskModel task)
        {
            if (task == null)
            {
                return BadRequest(new { success = false, message = "Invalid task data" });
            }

            var updatedTask = await _userRepo.UpdateTask(task);

            if (updatedTask != null)
            {


                if (task.c_assigned_to.HasValue)
                {
                    int userId = task.c_assigned_to.Value;

                }

                Console.WriteLine($"sender id {updatedTask.c_assigned_to} and recepientid = {updatedTask.c_created_by}");

                await _notificationService.SendTaskNotification(
                     senderId: updatedTask.c_assigned_to ?? 0,
                     recipientId: updatedTask.c_created_by ?? 0,
                     taskId: updatedTask.c_task_id,
                     taskTitle: updatedTask.c_title,
                     type: NotificationType.task_updated
                 );




                return Ok(new { success = true, message = "Task updated successfully", task = updatedTask });
            }
            else
            {
                return BadRequest(new { success = false, message = "Error while updating the task" });
            }
        }
        [HttpDelete("{taskId}")]
        public async Task<IActionResult> Delete(int taskId)
        {
            var task = await _userRepo.GetTaskById(taskId);
            if (task == null)
            {
                return NotFound(new { success = false, message = "Task not found" });
            }

            int? assignedUserId = task.c_assigned_to;

            var result = await _userRepo.DeleteTask(taskId);
            if (result)
            {
                // await _elasticService.DeleteTaskAsync(taskId);

                if (assignedUserId.HasValue)
                {
                    int userId = assignedUserId.Value;


                }

                return Ok(new { success = true, message = "Task deleted successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was an error while deleting the task" });
            }
        }

        [HttpGet("GetAllTasks/{id}")]
        public async Task<IActionResult> GetAllTasks(int id)
        {
            try
            {
                var tasks = await _userRepo.GetAllTasks(id);
                return Ok(tasks);
            }
            catch (Exception e)
            {
                return BadRequest(new { success = false, message = "Failed to fetch tasks", error = e.Message });
            }
        }

        
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

        //   [HttpPut("notifications/{notificationId}/read")]
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












    }
}