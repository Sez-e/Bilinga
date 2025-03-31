using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Models;

public class RiddleControllerTests
{
    [Fact]
    public void GetRandom_ReturnsRiddleText()
    {
        var mockService = new Mock<RiddleService>();
        mockService.Setup(s => s.GetRandomRiddle()).Returns(new Riddle
        {
            RiddleText = "What has keys but can't open locks?",
            Answer = "Piano",
            Translation = "У чего есть клавиши, но нет замков?"
        });

        var controller = new RiddleController(mockService.Object);
        
        var result = controller.GetRandom();
        
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("What has keys but can't open locks?", json["riddle"]);
    }

    [Fact]
    public void CheckAnswer_Correct_ReturnsTrue()
    {
        var mockService = new Mock<RiddleService>();
        mockService.Setup(s => s.CheckAnswer("Riddle?", "Answer")).Returns(true);

        var controller = new RiddleController(mockService.Object);

        var result = controller.CheckAnswer(new RiddleController.AnswerCheckRequest
        {
            Riddle = "Riddle?",
            Answer = "Answer"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = Assert.IsType<Dictionary<string, object>>(ok.Value);
        Assert.True((bool)json["correct"]);
        Assert.Equal("✅ Correct!", json["message"]);
    }

    [Fact]
    public void CheckAnswer_Incorrect_ReturnsFalse()
    {
        var mockService = new Mock<RiddleService>();
        mockService.Setup(s => s.CheckAnswer("Riddle?", "Wrong")).Returns(false);

        var controller = new RiddleController(mockService.Object);

        var result = controller.CheckAnswer(new RiddleController.AnswerCheckRequest
        {
            Riddle = "Riddle?",
            Answer = "Wrong"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = Assert.IsType<Dictionary<string, object>>(ok.Value);
        Assert.False((bool)json["correct"]);
        Assert.Equal("❌ Incorrect!", json["message"]);
    }

    [Fact]
    public void GetHint_RiddleFound_ReturnsTranslation()
    {
        var mockService = new Mock<RiddleService>();
        mockService.Setup(s => s.GetTranslation("Riddle?")).Returns("Загадка?");

        var controller = new RiddleController(mockService.Object);

        var result = controller.GetHint(new RiddleController.HintRequest
        {
            Riddle = "Riddle?"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("Загадка?", json["translation"]);
    }

    [Fact]
    public void GetHint_RiddleNotFound_Returns404()
    {
        var mockService = new Mock<RiddleService>();
        mockService.Setup(s => s.GetTranslation("Unknown")).Returns<string>(null);

        var controller = new RiddleController(mockService.Object);

        var result = controller.GetHint(new RiddleController.HintRequest
        {
            Riddle = "Unknown"
        });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var json = Assert.IsType<Dictionary<string, string>>(notFound.Value);
        Assert.Equal("Riddle not found.", json["message"]);
    }
}