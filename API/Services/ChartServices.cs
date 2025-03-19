using System;
using System.Threading.Tasks;
using Repositories.Interface;
using Repositories.Model;
using Repositories.Model.chat;

namespace API.Services
{
    public interface IChatService
    {
        Task<ChatMessage> SendMessageAsync(int senderId, int recipientId, string content);
        Task<List<ChatMessage>> GetChatHistoryAsync(int senderId, int recipientId);
    }

    public class ChatService : IChatService
    {
        private readonly UserInterface _userRepo;
        private readonly RabbitMQService _rabbitMQService;
        private readonly RedisService _redisService;

        public ChatService(UserInterface userRepo, RabbitMQService rabbitMQService, RedisService redisService)
        {
            _userRepo = userRepo;
            _rabbitMQService = rabbitMQService;
            _redisService = redisService;
        }

        public async Task<ChatMessage> SendMessageAsync(int senderId, int recipientId, string content)
        {
            var message = new ChatMessage
            {
                c_sender_id = senderId,
                c_recipient_id = recipientId,
                c_content = content,
                c_timestamp = DateTime.UtcNow
            };

            // Save to database
            var savedMessage = await _userRepo.SaveChatMessageAsync(message);

            // Publish to RabbitMQ
            _rabbitMQService.PublishChatMessage(savedMessage);

            // Cache in Redis
            await _redisService.CacheChatMessage(savedMessage);

            return savedMessage;
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(int senderId, int recipientId)
        {
            var redisHistory = await _redisService.GetChatHistory(senderId, recipientId);
            if (redisHistory.Any())
            {
                return redisHistory;
            }

            var dbHistory = await _userRepo.GetChatHistoryAsync(senderId, recipientId);
            foreach (var msg in dbHistory)
            {
                await _redisService.CacheChatMessage(msg);
            }
            return dbHistory;
        }
    }
}