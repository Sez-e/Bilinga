using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Data;
using backend.Models;
using System.Security.Claims;
namespace backend.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
        
        [Authorize]
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] UpdateUser updatedUser)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("id")?.Value;
            
            if (userIdClaim == null)
            {
                var debug = new
                {
                    IsAuthenticated = User.Identity.IsAuthenticated,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                    Headers = Request.Headers["Authorization"].ToString()
                };
                return Unauthorized(new { message = "User ID not found", debug });
            }

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(updatedUser.Password);
                user.IsTemporaryPassword = false;
            }

            if (!string.IsNullOrEmpty(updatedUser.Email))
            {
                user.Email = updatedUser.Email;
            }
            if (!string.IsNullOrEmpty(updatedUser.PhoneNumber))
            {
                user.PhoneNumber = updatedUser.PhoneNumber;
            }

            await _context.SaveChangesAsync();
            
            return Ok(new { 
                message = "Пользовательские данные успешно обновлены.",
                isTemporaryPassword = user.IsTemporaryPassword
            });
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();
            return Ok(new { message = "Пользователь успешно удалён." });
        }
    }
}