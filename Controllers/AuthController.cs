using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Data;
using backend.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly PasswordGeneratorService _passwordGenerator;

        public AuthController(
            AppDbContext context, 
            JwtService jwtService, 
            EmailService emailService,
            PasswordGeneratorService passwordGenerator)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _passwordGenerator = passwordGenerator;
        }

        public class RegisterRequest
        {
            [Required]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string Password { get; set; }
        }
        public class LoginRequest
        {
            [Required]
            public string Username { get; set; }

            [Required]
            public string Password { get; set; }
        }
        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверка на существование пользователя
            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
            {
                return Conflict("Пользователь с таким именем уже существует.");
            }

            // Создание нового пользователя
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Login), new { username = newUser.Username });
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return Unauthorized("Неверное имя пользователя или пароль.");
            }

            var token = _jwtService.GenerateToken(user.Username, user.Id);
            return Ok(new { Token = token });
        }

        public class ForgotPasswordRequest
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Возвращаем OK даже если пользователь не найден (для безопасности)
                return Ok(new { message = "If your email is registered, you will receive a temporary password." });
            }

            // Генерируем временный пароль
            var temporaryPassword = _passwordGenerator.GenerateTemporaryPassword();
            
            // Обновляем пароль пользователя
            user.Password = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            await _context.SaveChangesAsync();

            // Отправляем email с временным паролем
            var emailBody = $@"
                <h2>Temporary Password</h2>
                <p>Your temporary password is: <strong>{temporaryPassword}</strong></p>
                <p>Please change your password after logging in.</p>
                <p>If you didn't request a password reset, please contact support immediately.</p>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Your Temporary Password",
                emailBody
            );

            return Ok(new { message = "If your email is registered, you will receive a temporary password." });
        }
    }
}