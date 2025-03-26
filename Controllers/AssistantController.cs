using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;

        public ChatController()
        {
            _chatClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "openchat");
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] string userPrompt)
        {
            List<ChatMessage> chatHistory = new();
            chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

            var response = "";
            await foreach (var item in _chatClient.GetStreamingResponseAsync(chatHistory))
            {
                response += item.Text;
            }

            chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            return Ok(new { response });
        }
    }
}