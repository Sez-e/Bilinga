using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Note
    {
        public int Id { get; set; }
        
        [StringLength(30)]
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}