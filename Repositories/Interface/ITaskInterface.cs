using Repositories.Model.AdminModels;

namespace tmp;
public interface ITaskInterface
{
    Task<TaskModel> GetTaskById(int taskId);
    Task<IEnumerable<TaskModel>> GetAllTasks();
    Task <TaskModel> AddTask(TaskModel task);
    Task<TaskModel> UpdateTask(TaskModel taskChanges);
    Task<bool> DeleteTask(int taskId);
}