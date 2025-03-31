using Microsoft.AspNetCore.Mvc;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var notes = _context.Notes.ToList();
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var note = _context.Notes.Find(id);
            if (note == null)
            {
                return NotFound();
            }
            return Ok(note);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateNote createdNote)
        {
            if (string.IsNullOrEmpty(createdNote.Title))
            {
                return BadRequest("Необходимо название заметки.");
            }
            
            var newNote = new Note
            {
                Title = createdNote.Title,
                Content = createdNote.Content,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Notes.Add(newNote);
            _context.SaveChanges();
            
            return CreatedAtAction(nameof(GetById), new { id = newNote.Id }, newNote);
        }


        [HttpPatch("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateNote updatedNote)
        {
            var note = _context.Notes.Find(id);
            if (note == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updatedNote.Title))
            {
                note.Title = updatedNote.Title;
            }
            if (!string.IsNullOrEmpty(updatedNote.Content))
            {
                note.Content = updatedNote.Content;
            }
            note.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            return Ok(note);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var note = _context.Notes.Find(id);
            if (note == null)
            {
                return NotFound();
            }

            _context.Notes.Remove(note);
            _context.SaveChanges();
            return Ok(new { message = "Заметка успешно удалена." });
        }
    }
}