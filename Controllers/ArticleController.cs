using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly ArticleService _articleService;

        public ArticlesController(ArticleService articleService)
        {
            _articleService = articleService;
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
    }
}