using System.Text.Json.Serialization;

namespace bili.Models
{
    public class DictionaryOldFormat
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("en")] public string En { get; set; } = string.Empty; // Фикс для пустых значений

        [JsonPropertyName("ru")] public string Ru { get; set; } = string.Empty;

        [JsonPropertyName("tr")] public string Tr { get; set; } = string.Empty;
    }
}