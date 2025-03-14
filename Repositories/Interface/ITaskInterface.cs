
namespace tmp;
public interface ITaskInterface
{
    Task<t_tasks> GetTaskById(int taskId);
    Task<IEnumerable<t_tasks>> GetAllTasks();
    Task <t_tasks> AddTask(t_tasks task);
    Task<t_tasks> UpdateTask(t_tasks taskChanges);
    Task<bool> DeleteTask(int taskId);
}