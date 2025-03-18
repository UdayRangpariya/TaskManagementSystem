using System;
using System.Collections.Generic;
using System.Linq;
using Repositories.Model.AdminModels;

using System.Threading.Tasks;

namespace Repositories.Interface
{
    public interface AdminInterface
    {
        #region User Methods
        Task<List<UserModel>> GetAllUsers();
        Task<UserModel> GetUserById(int userId);
        #endregion

        #region Task Methods
        Task<List<TaskModel>> GetAllTasks();
        Task<List<TaskModel>> GetTasksByUserId(int userId);
        Task<int> AssignTask(TaskModel newTask);
          public  Task<(int c_created_by, int c_assigned_to, string c_title)> DeleteTaskAsync(int taskId);
        public  Task<int> UpdateTask(TaskModel task);
        public Task<List<TaskModel>> GetTasksByCreatorAndAssignee(int userId, int AdminId);
       

        #endregion
    }


}