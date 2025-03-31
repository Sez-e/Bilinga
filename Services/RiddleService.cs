using System.Text.Json;
using backend.Models;

public class RiddleService
{
    private readonly List<Riddle> _riddles;

    public RiddleService()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "riddle.json");
        var json = File.ReadAllText(path);
        _riddles = JsonSerializer.Deserialize<List<Riddle>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Riddle>();
    }

    public Riddle GetRandomRiddle()
    {
        var rnd = new Random();
        return _riddles[rnd.Next(_riddles.Count)];
    }

    public bool CheckAnswer(string riddleText, string userAnswer)
    {
        var riddle = _riddles.FirstOrDefault(r => r.RiddleText.Equals(riddleText, StringComparison.OrdinalIgnoreCase));
        return riddle != null && riddle.Answer.Trim().Equals(userAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public string? GetTranslation(string riddleText)
    {
        return _riddles.FirstOrDefault(r => r.RiddleText.Equals(riddleText, StringComparison.OrdinalIgnoreCase))?.Translation;
    }
}