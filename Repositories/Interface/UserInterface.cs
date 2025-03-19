using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Model.AdminModels;
using Repositories.Model.chat;

namespace Repositories.Interface
{
    public interface UserInterface
    {
        public Task<TaskModel> UpdateTask(TaskModel task);

        public Task<bool> DeleteTask(int taskId);
        public Task<TaskModel> AddTask(TaskModel task);
        public Task<IEnumerable<TaskModel>> GetAllTasks(int id);
        public Task<TaskModel> GetTaskById(int taskId);
        Task<ChatMessage> SaveChatMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetChatHistoryAsync(int senderId, int recipientId);
      public  Task<List<UserModel>> GetAllUsers();

    }
}