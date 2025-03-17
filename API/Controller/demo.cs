using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // For IFormFile
using Elastic.Clients.Elasticsearch;
namespace tmp
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskInterface _taskrepo;
        private readonly ElasticsearchService _elasticService;
        public TaskController(ITaskInterface taskrepo, ElasticsearchService elasticService)
        {
            _taskrepo = taskrepo;
            _elasticService = elasticService;
        }
        [HttpGet("task/{name}")]
        public async Task<IActionResult> SearchContact(string name)
        {
            var task = await _elasticService.SearchTaskAsync(name);
            return task is null ? NotFound() : Ok(task);
        }

        // GET: api/Task/1
        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTaskById(int taskId)
        {
            var task = await _taskrepo.GetTaskById(taskId);
            if (task == null)
            {
                return NotFound(new { success = false, message = "Task not found" });
            }

            return Ok(task);
        }

        // POST: api/Task
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] t_tasks task)
        {
            if (task == null)
            {
                return BadRequest(new { success = false, message = "Invalid task data" });
            }

            // Insert the task into the database first
            var addedTask = await _taskrepo.AddTask(task);
            if (addedTask != null)
            {
              
                task.c_task_id = addedTask.c_task_id;

               
                await _elasticService.IndexTaskAsync(task);

                return Ok(new { success = true, message = "Task inserted successfully", task = addedTask });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was an error while adding the task" });
            }
        }


        // PUT: api/Task
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] t_tasks task)
        {
            if (task == null)
            {
                return BadRequest(new { success = false, message = "Invalid task data" });
            }

            var updatedTask = await _taskrepo.UpdateTask(task);
            if (updatedTask != null)
            {
                await _elasticService.UpdateTaskAsync(task);
                return Ok(new { success = true, message = "Task updated successfully", task = updatedTask });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was an error while updating the task" });
            }
        }

        // DELETE: api/Task/1
        [HttpDelete("{taskId}")]
        //[Authorize]  // Assuming you want authorization here
        public async Task<IActionResult> Delete(int taskId)
        {
            var result = await _taskrepo.DeleteTask(taskId);
            if (result)
            {
                await _elasticService.DeleteTaskAsync(taskId);
                return Ok(new { success = true, message = "Task deleted successfully" });
            }
            else
            {
                return BadRequest(new { success = false, message = "There was an error while deleting the task" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            try
            {
                var tasks = await _taskrepo.GetAllTasks();
                return Ok(tasks);
            }
            catch (Exception e)
            {
                return BadRequest(new { success = false, message = "Failed to fetch tasks", error = e.Message });
            }
        }
    }
}
