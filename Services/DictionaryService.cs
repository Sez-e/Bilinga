using System.Text.Json;
using backend.Models;

public class DictionaryService
{
    private readonly string _jsonFilePath;

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public DictionaryService(string? jsonFilePath = null)
    {
        _jsonFilePath = jsonFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dictionary.json");
    }

    public async Task<List<DictionaryEntry>> LoadDictionaryAsync()
    {
        if (!File.Exists(_jsonFilePath))
        {
            Console.WriteLine($"❌ Файл не найден: {_jsonFilePath}");
            return new List<DictionaryEntry>();
        }

        try
        {
            using var stream = new FileStream(_jsonFilePath, FileMode.Open, FileAccess.Read);
            var result = await JsonSerializer.DeserializeAsync<List<DictionaryEntry>>(stream, _options);
            return result ?? new List<DictionaryEntry>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка при чтении JSON: {ex.Message}");
            return new List<DictionaryEntry>();
        }
    }

    public async Task SaveDictionaryAsync(List<DictionaryEntry> dictionary)
    {
        try
        {
            using var stream = new FileStream(_jsonFilePath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, dictionary, _options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка при сохранении JSON: {ex.Message}");
        }
    }

    public async Task<DictionaryEntry?> FindAsync(string query)
    {
        var dictionary = await LoadDictionaryAsync();
        return dictionary.FirstOrDefault(d =>
            string.Equals(d.Word, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.Translation, query, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<DictionaryEntry> AddEntryAsync(DictionaryEntry entry)
    {
        var dictionary = await LoadDictionaryAsync();
        dictionary.Add(entry);
        await SaveDictionaryAsync(dictionary);
        return entry;
    }

    public async Task<DictionaryEntry?> UpdateEntryAsync(string word, DictionaryEntry updated)
    {
        var dictionary = await LoadDictionaryAsync();
        var entry = dictionary.FirstOrDefault(d =>
            d.Word.Equals(word, StringComparison.OrdinalIgnoreCase) ||
            d.Translation.Equals(word, StringComparison.OrdinalIgnoreCase));

        if (entry != null)
        {
            entry.Translation = updated.Translation;
            entry.Transcription = updated.Transcription;
            await SaveDictionaryAsync(dictionary);
        }

        return entry;
    }

    public async Task<bool> DeleteEntryAsync(string word)
    {
        var dictionary = await LoadDictionaryAsync();
        var entry = dictionary.FirstOrDefault(d =>
            d.Word.Equals(word, StringComparison.OrdinalIgnoreCase) ||
            d.Translation.Equals(word, StringComparison.OrdinalIgnoreCase));

        if (entry != null)
        {
            dictionary.Remove(entry);
            await SaveDictionaryAsync(dictionary);
            return true;
        }

        return false;
    }
}
