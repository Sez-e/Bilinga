namespace backend.Models
{
    public class ChatHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string UserMessage { get; set; }
        public required string AssistantMessage { get; set; }
        public DateTime Timestamp { get; set; }
        
        public User User { get; set; }
    }
}