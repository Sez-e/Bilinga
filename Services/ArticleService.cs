using System.Text.Json;
using backend.Models;

public class ArticleService
{
    private const string JsonFilePath = "articles.json";

    public async Task<List<Article>> LoadArticlesAsync()
    {
        using (var stream = new FileStream(JsonFilePath, FileMode.Open, FileAccess.Read))
        {
            return await JsonSerializer.DeserializeAsync<List<Article>>(stream);
        }
    }
}