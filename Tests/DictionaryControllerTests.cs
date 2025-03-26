using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using bili.Controllers;
using bili.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class DictionaryControllerTests
{
    private readonly DictionaryController _controller;
    private readonly string _testFilePath = "test_dictionary.json";

    public DictionaryControllerTests()
    {
        _controller = new DictionaryController();
    }

    private void SetupTestFile(string content)
    {
        File.WriteAllText(_testFilePath, content, Encoding.UTF8);
    }

    private void CleanupTestFile()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void Get_ReturnsEmptyList_WhenNoFile()
    {
        CleanupTestFile();
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        Assert.IsType<List<Dictionary>>(result.Value);
        Assert.Empty((List<Dictionary>)result.Value);
    }

    [Fact]
    public void Get_ReturnsListOfWords()
    {
        SetupTestFile("[{\"Term\":\"Hello\", \"Translate\":\"Привет\", \"Transcription\":\"həˈloʊ\", \"Synonyms\":[]}]");
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        Assert.IsType<List<Dictionary>>(result.Value);
        Assert.Single((List<Dictionary>)result.Value);
        CleanupTestFile();
    }

    [Fact]
    public void Search_FindsExistingWord()
    {
        SetupTestFile("[{\"Term\":\"Hello\", \"Translate\":\"Привет\", \"Transcription\":\"həˈloʊ\", \"Synonyms\":[]}]");
        var result = _controller.Search("Hello") as OkObjectResult;
        Assert.NotNull(result);
        var word = result.Value as Dictionary;
        Assert.Equal("Hello", word.Term);
        CleanupTestFile();
    }

    [Fact]
    public void Search_ReturnsNotFoundForMissingWord()
    {
        var result = _controller.Search("NonExistent") as NotFoundObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Create_AddsNewWord()
    {
        var newEntry = new Dictionary { Term = "Test", Translate = "Тест", Transcription = "tɛst", Synonyms = new List<string>() };
        var result = _controller.Create(newEntry) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal("Search", result.ActionName);
    }

    [Fact]
    public void Create_FailsOnNullEntry()
    {
        var result = _controller.Create(null) as BadRequestResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Create_FailsOnEmptyTerm()
    {
        var newEntry = new Dictionary { Term = "", Translate = "Тест", Transcription = "tɛst", Synonyms = new List<string>() };
        var result = _controller.Create(newEntry) as BadRequestResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Create_LongTerm_Success()
    {
        var newEntry = new Dictionary { Term = new string('A', 1000), Translate = "Тест", Transcription = "tɛst", Synonyms = new List<string>() };
        var result = _controller.Create(newEntry) as CreatedAtActionResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Update_ChangesExistingWord()
    {
        SetupTestFile("[{\"Term\":\"Hello\", \"Translate\":\"Привет\", \"Transcription\":\"həˈloʊ\", \"Synonyms\":[]}]");
        var updatedEntry = new Dictionary { Term = "Hello", Translate = "Здравствуйте", Transcription = "həˈloʊ", Synonyms = new List<string>() };
        var result = _controller.Update("Hello", updatedEntry) as OkObjectResult;
        Assert.NotNull(result);
        var updatedWord = result.Value as Dictionary;
        Assert.Equal("Здравствуйте", updatedWord.Translate);
        CleanupTestFile();
    }

    [Fact]
    public void Update_ReturnsBadRequestOnNullEntry()
    {
        var result = _controller.Update("Hello", null) as BadRequestResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void Delete_RemovesWord()
    {
        SetupTestFile("[{\"Term\":\"Hello\", \"Translate\":\"Привет\", \"Transcription\":\"həˈloʊ\", \"Synonyms\":[]}]");
        var result = _controller.Delete("Hello") as OkObjectResult;
        Assert.NotNull(result);
        CleanupTestFile();
    }

    [Fact]
    public void Delete_ReturnsNotFoundForMissingWord()
    {
        var result = _controller.Delete("NonExistent") as NotFoundResult;
        Assert.NotNull(result);
    }

    [Fact]
    public void LoadDictionary_ReturnsEmptyList_OnInvalidJson()
    {
        SetupTestFile("Invalid JSON Data");
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        Assert.IsType<List<Dictionary>>(result.Value);
        Assert.Empty((List<Dictionary>)result.Value);
        CleanupTestFile();
    }

    [Fact]
    public void LoadDictionary_LargeJsonFile()
    {
        var largeData = new List<Dictionary>();
        for (int i = 0; i < 100000; i++)
        {
            largeData.Add(new Dictionary { Term = "Word" + i, Translate = "Перевод" + i, Transcription = "trans" + i, Synonyms = new List<string>() });
        }
        SetupTestFile(JsonSerializer.Serialize(largeData));
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        Assert.IsType<List<Dictionary>>(result.Value);
        Assert.Equal(100000, ((List<Dictionary>)result.Value).Count);
        CleanupTestFile();
    }
}

