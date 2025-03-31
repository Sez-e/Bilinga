using Microsoft.AspNetCore.Mvc;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using backend.Data;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly ArticleService _articleService;
        private readonly AppDbContext _context;

        public ArticlesController(ArticleService articleService, AppDbContext context)
        {
            _articleService = articleService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string languageLevel = null, string titleSearch = null)
        {
            var articles = await _articleService.LoadArticlesAsync(); // Загрузка статей из JSON

            // Фильтрация по уровню языка
            if (!string.IsNullOrEmpty(languageLevel))
            {
                articles = articles.Where(a => a.Level.Equals(languageLevel, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Поиск по названию
            if (!string.IsNullOrEmpty(titleSearch))
            {
                articles = articles.Where(a => a.Title.Contains(titleSearch, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(articles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var articles = await _articleService.LoadArticlesAsync(); // Загрузка статей из JSON
            var article = articles.FirstOrDefault(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }
            return Ok(article);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateArticle createdArticle)
        {
            if (string.IsNullOrEmpty(createdArticle.Title) || 
                string.IsNullOrEmpty(createdArticle.Content) || 
                string.IsNullOrEmpty(createdArticle.Level))
            {
                return BadRequest("Для создания статьи необходимо ввести название, содержание и уровень языка.");
            }

            // Получаем UserId из контекста пользователя
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("Пользователь не авторизован.");
            }

            var userId = int.Parse(userIdClaim);
            
            // Находим пользователя для получения username
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return BadRequest("Пользователь не найден.");
            }

            try
            {
                var article = new Article
                {
                    Title = createdArticle.Title,
                    Content = createdArticle.Content,
                    Level = createdArticle.Level,
                    Author = user.Username
                };
                
                var newArticle = await _articleService.CreateArticleAsync(article, user.Username);
                return CreatedAtAction(nameof(GetById), new { id = newArticle.Id }, newArticle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateArticle updatedArticle)
        {
            // Получаем UserId из контекста пользователя
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("Пользователь не авторизован.");
            }

            var userId = int.Parse(userIdClaim);
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return BadRequest("Пользователь не найден.");
            }

            // Проверяем, существует ли статья и является ли пользователь её автором
            var existingArticles = await _articleService.LoadArticlesAsync();
            var existingArticle = existingArticles.FirstOrDefault(a => a.Id == id);
            
            if (existingArticle == null)
            {
                return NotFound();
            }

            if (existingArticle.Author != user.Username)
            {
                return Forbid("Вы можете редактировать только свои статьи.");
            }

            try
            {
                var article = new Article
                {
                    Title = updatedArticle.Title ?? existingArticle.Title,
                    Content = updatedArticle.Content ?? existingArticle.Content,
                    Level = updatedArticle.Level ?? existingArticle.Level,
                    Author = existingArticle.Author // Добавляем Author при обновлении
                };
                
                var result = await _articleService.UpdateArticleAsync(id, article);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            // Получаем UserId из контекста пользователя
            var userIdClaim = User.FindFirst("id")?.Value;
            if (userIdClaim == null)
            {
                return Unauthorized("Пользователь не авторизован.");
            }

            var userId = int.Parse(userIdClaim);
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return BadRequest("Пользователь не найден.");
            }

            // Проверяем, существует ли статья и является ли пользователь её автором
            var existingArticles = await _articleService.LoadArticlesAsync();
            var existingArticle = existingArticles.FirstOrDefault(a => a.Id == id);
            
            if (existingArticle == null)
            {
                return NotFound();
            }

            if (existingArticle.Author != user.Username)
            {
                return Forbid("Вы можете удалять только свои статьи.");
            }

            try
            {
                var result = await _articleService.DeleteArticleAsync(id);
                if (result)
                {
                    return Ok(new { message = "Статья успешно удалена." });
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}