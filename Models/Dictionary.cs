public class Dictionary
{
    public int Id { get; set; }

    public string Term { get; set; }
    public string Translate { get; set; }
    public string Transcription { get; set; }
    public List<string> Synonyms { get; set; } = new List<string>();
}