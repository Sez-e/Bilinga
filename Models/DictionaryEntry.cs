namespace backend.Models
{
    public class DictionaryEntry
    {
        public int Id { get; set; }
        public required string Word { get; set; }
        public required string Translation { get; set; }
        public required string Transcription { get; set; }
    }
}