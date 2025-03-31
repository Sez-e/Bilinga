using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.AspNetCore.Authorization;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatClient _chatClient;
        private readonly AppDbContext _context;
        private const string WelcomeMessageEn = "Hi! I'm AInga, your English learning assistant. You can ask me any question or just chat with me. I will communicate with you in English. But if something is unclear, you can ask me to translate the message or rephrase it. I will also help you construct grammatically and logically correct sentences in English and correct mistakes.";
        private const string WelcomeMessageRu = "Привет! Я AInga, твой помощник в изучении английского языка. Ты можешь задать мне любой вопрос или попросить просто поболтать с тобой. Я буду общаться с тобой на английском языке. Но если тебе что-то непонятно, то ты можешь попросить меня перевести сообщение или переформулировать его. Я также помогу тебе составлять грамматически и логически правильные предложения на английском и исправлять ошибки.";

        public ChatController(AppDbContext context)
        {
            _chatClient = new OllamaChatClient(new Uri("http://localhost:11434/"), "openchat");
            _context = context;
        }

        [HttpGet("welcome")]
        [Authorize]
        public async Task<IActionResult> GetInitialMessage()
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            
            // Проверка истории диалогов
            var hasHistory = await _context.ChatHistory.AnyAsync(h => h.UserId == userId);
            if (hasHistory)
            {
                return Ok(new { message = "Welcome back!" });
            }

            var welcomeMessage = $"{WelcomeMessageEn}\n\n{WelcomeMessageRu}";
            
            // Сохранение приветственнго сообщения в историю
            var chatHistory = new ChatHistory
            {
                UserId = userId,
                UserMessage = "Start conversation",
                AssistantMessage = welcomeMessage,
                Timestamp = DateTime.UtcNow
            };
            
            _context.ChatHistory.Add(chatHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = welcomeMessage });
        }

        [HttpPost("message")]
public async Task<IActionResult> SendMessage([FromBody] string userPrompt)
{
    List<ChatMessage> chatHistory = new();
    
    // Базовый системный промпт
    chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.BaseSystemPrompt));

    // Определяем тип запроса пользователя и добавляем соответствующий промпт
    if (userPrompt.ToLower().Contains("перевед") || userPrompt.ToLower().Contains("translat"))
    {
        chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.TranslationPrompt));
    }
    else if (userPrompt.ToLower().Contains("грамматик") || userPrompt.ToLower().Contains("grammar"))
    {
        chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.GrammarExplanationPrompt));
    }
    else if (userPrompt.ToLower().Contains("поговор") || userPrompt.ToLower().Contains("chat"))
    {
        chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.ConversationPrompt));
    }
    else if (userPrompt.ToLower().Contains("слов") || userPrompt.ToLower().Contains("vocabulary"))
    {
        chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.VocabularyPrompt));
    }
    else if (userPrompt.ToLower().Contains("произнош") || userPrompt.ToLower().Contains("pronunciation"))
    {
        chatHistory.Add(new ChatMessage(ChatRole.System, PromptService.PronunciationPrompt));
    }

    chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    var response = "";
    await foreach (var item in _chatClient.GetStreamingResponseAsync(chatHistory))
    {
        response += item.Text;
    }

    // Сохранение истории для авторизованных пользователей
    if (User.Identity.IsAuthenticated)
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (userIdClaim != null)
        {
            var userId = int.Parse(userIdClaim);
            var chatHistoryEntry = new ChatHistory
            {
                UserId = userId,
                UserMessage = userPrompt,
                AssistantMessage = response,
                Timestamp = DateTime.UtcNow
            };
            
            _context.ChatHistory.Add(chatHistoryEntry);
            await _context.SaveChangesAsync();
        }
    }

    return Ok(new { response });
}
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetHistory()
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            var userId = int.Parse(userIdClaim);
            var history = await _context.ChatHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.Timestamp)
                .Select(h => new
                {
                    h.UserMessage,
                    h.AssistantMessage,
                    h.Timestamp
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}