using System.Text;
using System.Text.Json;
using backend.Models;
using backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class DictionaryControllerTests : IDisposable
{
    private readonly string _testFilePath = "Data/dictionary.json";
    private readonly DictionaryService _service;
    private readonly DictionaryController _controller;

    public DictionaryControllerTests()
    {
        Directory.CreateDirectory("Data");
        _service = new DictionaryService(_testFilePath);
        _controller = new DictionaryController(_service);
    }

    private void SetupTestFile(string content)
    {
        File.WriteAllText(_testFilePath, content, Encoding.UTF8);
    }

    private void CleanupTestFile()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    public void Dispose() => CleanupTestFile();

    [Fact]
    public async Task Get_ReturnsEmptyList_WhenNoFile()
    {
        CleanupTestFile();
        var result = await _controller.Get() as OkObjectResult;
        var list = Assert.IsType<List<DictionaryEntry>>(result!.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Get_ReturnsListOfWords()
    {
        SetupTestFile("[{\"word\":\"Hello\",\"translation\":\"Привет\",\"transcription\":\"[həˈloʊ]\"}]");
        var result = await _controller.Get() as OkObjectResult;
        var list = Assert.IsType<List<DictionaryEntry>>(result!.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task Search_FindsExistingWord()
    {
        SetupTestFile("[{\"word\":\"Hello\",\"translation\":\"Привет\",\"transcription\":\"[həˈloʊ]\"}]");
        var result = await _controller.Search("Hello") as OkObjectResult;
        var word = Assert.IsType<DictionaryEntry>(result!.Value);
        Assert.Equal("Hello", word.Word);
    }

    [Fact]
    public async Task Search_ReturnsNotFoundForMissingWord()
    {
        CleanupTestFile();
        var result = await _controller.Search("NonExistent");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_AddsNewWord()
    {
        var entry = new DictionaryEntry { Word = "Test", Translation = "Тест", Transcription = "[tɛst]" };
        var result = await _controller.Create(entry) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal("Search", result.ActionName);
    }

    [Fact]
    public async Task Create_FailsOnNullEntry()
    {
        var result = await _controller.Create(null);
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Update_ChangesExistingWord()
    {
        SetupTestFile("[{\"word\":\"Hello\",\"translation\":\"Привет\",\"transcription\":\"[həˈloʊ]\"}]");
        var updated = new DictionaryEntry
        {
            Word = "Hello",
            Translation = "Здравствуйте",
            Transcription = "[zdrɑːˈstvʊjɪtʲe]"
        };
        var result = await _controller.Update("Hello", updated) as OkObjectResult;
        var word = Assert.IsType<DictionaryEntry>(result!.Value);
        Assert.Equal("Здравствуйте", word.Translation);
    }

    [Fact]
    public async Task Update_ReturnsBadRequestOnNull()
    {
        var result = await _controller.Update("Hello", null);
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_RemovesWord()
    {
        SetupTestFile("[{\"word\":\"Hello\",\"translation\":\"Привет\",\"transcription\":\"[həˈloʊ]\"}]");
        var result = await _controller.Delete("Hello");
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundForMissingWord()
    {
        CleanupTestFile();
        var result = await _controller.Delete("Missing");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task LoadDictionary_LargeJsonFile()
    {
        var entries = Enumerable.Range(0, 1000).Select(i => new DictionaryEntry
        {
            Word = $"Word{i}",
            Translation = $"Перевод{i}",
            Transcription = $"[tr{i}]"
        }).ToList();

        SetupTestFile(JsonSerializer.Serialize(entries));
        var result = await _controller.Get() as OkObjectResult;
        var list = Assert.IsType<List<DictionaryEntry>>(result!.Value);
        Assert.Equal(1000, list.Count);
    }
}
