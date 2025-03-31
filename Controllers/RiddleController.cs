using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RiddleController : ControllerBase
{
    private readonly RiddleService _service;

    public RiddleController(RiddleService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetRandom()
    {
        var riddle = _service.GetRandomRiddle();
        return Ok(new
        {
            riddle = riddle.RiddleText // только английский текст
        });
    }

    public class AnswerCheckRequest
    {
        public string Riddle { get; set; }
        public string Answer { get; set; }
    }

    [HttpPost("answer")]
    public IActionResult CheckAnswer([FromBody] AnswerCheckRequest request)
    {
        if (_service.CheckAnswer(request.Riddle, request.Answer))
            return Ok(new { correct = true, message = "✅ Correct!" });

        return Ok(new { correct = false, message = "❌ Incorrect!" });
    }

    public class HintRequest
    {
        public string Riddle { get; set; }
    }

    [HttpPost("hint")]
    public IActionResult GetHint([FromBody] HintRequest request)
    {
        var translation = _service.GetTranslation(request.Riddle);

        if (translation == null)
            return NotFound(new { message = "Riddle not found." });

        return Ok(new { translation });
    }
}