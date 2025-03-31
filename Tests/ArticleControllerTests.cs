using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Controllers;
using backend.Data;
using backend.Models;


namespace ArticlesControllerTests
{
    public class ArticlesControllerTests
    {
        private ArticlesController GetControllerWithUser(Mock<ArticleService> mockService, AppDbContext context,
            int userId,
            string username)
        {
            var controller = new ArticlesController(mockService.Object, context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("id", userId.ToString())
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        private AppDbContext GetDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task Get_FiltersByLevelAndTitle()
        {
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article>
            {
                new Article { Id = 1, Title = "Test", Level = "A2", Content = "Some content", Author = "testuser" },
                new Article { Id = 2, Title = "Advanced grammar", Level = "C1", Content = "Advanced content", Author = "grammarian" }
            });

            var controller = new ArticlesController(mockService.Object, GetDb("Get_Filters"));

            var result = await controller.Get("A2", "test");

            var ok = Assert.IsType<OkObjectResult>(result);
            var articles = Assert.IsAssignableFrom<IEnumerable<Article>>(ok.Value);
            Assert.Single(articles);
        }

        [Fact]
        public async Task GetById_Found_ReturnsArticle()
        {
            var article = new Article { Id = 5, Title = "X", Level = "B1", Content = "Sample content", Author = "testuser" };
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article> { article });

            var controller = new ArticlesController(mockService.Object, GetDb("GetById_Found"));

            var result = await controller.GetById(5);
            var ok = Assert.IsType<OkObjectResult>(result);
            var found = Assert.IsType<Article>(ok.Value);
            Assert.Equal("X", found.Title);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article>());

            var controller = new ArticlesController(mockService.Object, GetDb("GetById_NotFound"));
            var result = await controller.GetById(123);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ValidArticle_ReturnsCreated()
        {
            var db = GetDb("Create_ValidArticle");
            db.Users.Add(new User { Id = 1, Username = "alice", Email = "alice@example.com", Password = "123", Score = 0 });
            db.SaveChanges();

            var created = new Article { Id = 10, Title = "New", Content = "X", Level = "A1", Author = "alice" };

            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.CreateArticleAsync(It.IsAny<Article>(), "alice")).ReturnsAsync(created);

            var controller = GetControllerWithUser(mockService, db, 1, "alice");

            var result = await controller.Create(new CreateArticle { Title = "New", Content = "X", Level = "A1" });

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var article = Assert.IsType<Article>(createdResult.Value);
            Assert.Equal("alice", article.Author);
        }

        [Fact]
        public async Task Create_MissingFields_ReturnsBadRequest()
        {
            var controller = GetControllerWithUser(new Mock<ArticleService>(), GetDb("Create_Bad"), 1, "bob");

            var result = await controller.Create(new CreateArticle { Title = "", Content = "", Level = "" });
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Title, Content and Level are required.", bad.Value);
        }

        [Fact]
        public async Task Create_UnauthorizedUser_ReturnsUnauthorized()
        {
            var controller = new ArticlesController(new Mock<ArticleService>().Object, GetDb("Create_Unauthorized"));
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() // No User
            };

            var result = await controller.Create(new CreateArticle { Title = "X", Content = "Y", Level = "B2" });
            var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User is not authenticated.", unauth.Value);
        }

        [Fact]
        public async Task Update_NotOwner_ReturnsForbid()
        {
            var db = GetDb("Update_NotOwner");
            db.Users.Add(new User { Id = 2, Username = "bob", Email = "bob@example.com", Password = "123", Score = 0 });
            db.SaveChanges();

            var article = new Article { Id = 10, Title = "Title", Content = "Text", Author = "alice", Level = "B1" };
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article> { article });

            var controller = GetControllerWithUser(mockService, db, 2, "bob");

            var result = await controller.Update(10, new UpdateArticle { Title = "Hacked" });

            var forbidden = Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Delete_Success_ReturnsOk()
        {
            var db = GetDb("Delete_Success");
            db.Users.Add(new User { Id = 1, Username = "alice", Email = "alice@example.com", Password = "123", Score = 0 });
            db.SaveChanges();

            var article = new Article { Id = 77, Title = "Title", Content = "Article content", Author = "alice", Level = "A1" };
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article> { article });
            mockService.Setup(s => s.DeleteArticleAsync(77)).ReturnsAsync(true);

            var controller = GetControllerWithUser(mockService, db, 1, "alice");

            var result = await controller.Delete(77);

            var ok = Assert.IsType<OkObjectResult>(result);
            var json = Assert.IsAssignableFrom<Dictionary<string, string>>(ok.Value);
            Assert.Equal("Article successfully deleted.", json["message"]);
        }

        [Fact]
        public async Task Delete_NotOwner_ReturnsForbid()
        {
            var db = GetDb("Delete_NotOwner");
            db.Users.Add(new User { Id = 3, Username = "bob", Email = "bob@example.com", Password = "123", Score = 0 });
            db.SaveChanges();

            var article = new Article { Id = 9, Title = "T", Content = "Some content", Author = "alice", Level = "C1" };
            var mockService = new Mock<ArticleService>();
            mockService.Setup(s => s.LoadArticlesAsync()).ReturnsAsync(new List<Article> { article });

            var controller = GetControllerWithUser(mockService, db, 3, "bob");
            var result = await controller.Delete(9);

            Assert.IsType<ForbidResult>(result);
        }
    }
}