using Microsoft.AspNetCore.Mvc;
using API.Services;
using Repositories.Model;

namespace API.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var message = await _chatService.SendMessageAsync(request.SenderId, request.RecipientId, request.Content);
            return Ok(new { success = true, message = "Message sent", data = message });
        }

        [HttpGet("history/{senderId}/{recipientId}")]
        public async Task<IActionResult> GetChatHistory(int senderId, int recipientId)
        {
            var history = await _chatService.GetChatHistoryAsync(senderId, recipientId);
            return Ok(history);
        }
    }

    public class SendMessageRequest
    {
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Content { get; set; }
    }
}