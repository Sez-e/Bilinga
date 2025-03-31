using System.Text.Json;
using backend.Models;

public class ArticleService
{
    private const string JsonFilePath = "articles.json";
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true // Для красивого форматирования JSON
    };

    public async Task<List<Article>> LoadArticlesAsync()
    {
        using (var stream = new FileStream(JsonFilePath, FileMode.Open, FileAccess.Read))
        {
            return await JsonSerializer.DeserializeAsync<List<Article>>(stream);
        }
    }

    public async Task SaveArticlesAsync(List<Article> articles)
    {
        using (var stream = new FileStream(JsonFilePath, FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(stream, articles, _options);
        }
    }

    public async Task<Article> CreateArticleAsync(Article newArticle, string username)
    {
        var articles = await LoadArticlesAsync();
        
        int maxId = articles.Max(a => a.Id);
        newArticle.Id = maxId + 1;
        
        // Устанавливаем автора
        newArticle.Author = username;
        
        articles.Add(newArticle);
        await SaveArticlesAsync(articles);
        
        return newArticle;
    }

    public async Task<Article> UpdateArticleAsync(int id, Article updatedArticle)
    {
        var articles = await LoadArticlesAsync();
        var article = articles.FirstOrDefault(a => a.Id == id);
        
        if (article != null)
        {
            article.Title = updatedArticle.Title;
            article.Content = updatedArticle.Content;
            article.Level = updatedArticle.Level;
            
            await SaveArticlesAsync(articles);
        }
        
        return article;
    }

    public async Task<bool> DeleteArticleAsync(int id)
    {
        var articles = await LoadArticlesAsync();
        var article = articles.FirstOrDefault(a => a.Id == id);
        
        if (article != null)
        {
            articles.Remove(article);
            await SaveArticlesAsync(articles);
            return true;
        }
        
        return false;
    }
}