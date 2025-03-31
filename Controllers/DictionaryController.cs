using Microsoft.AspNetCore.Mvc;
using backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DictionaryController : ControllerBase
    {
        private readonly DictionaryService _service;

        public DictionaryController(DictionaryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var dictionary = await _service.LoadDictionaryAsync();
            return Ok(dictionary);
        }

        [HttpGet("search/{query}")]
        public async Task<IActionResult> Search(string query)
        {
            var result = await _service.FindAsync(query);
            if (result == null)
                return NotFound(new { message = "Слово не найдено" });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DictionaryEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Word) || string.IsNullOrWhiteSpace(entry.Translation))
                return BadRequest();

            var added = await _service.AddEntryAsync(entry);
            return CreatedAtAction(nameof(Search), new { query = added.Word }, added);
        }

        [HttpPatch("{word}")]
        public async Task<IActionResult> Update(string word, [FromBody] DictionaryEntry updated)
        {
            if (updated == null)
                return BadRequest();

            var result = await _service.UpdateEntryAsync(word, updated);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{word}")]
        public async Task<IActionResult> Delete(string word)
        {
            var success = await _service.DeleteEntryAsync(word);
            if (!success)
                return NotFound();

            return Ok(new { message = "Запись удалена" });
        }
    }
}