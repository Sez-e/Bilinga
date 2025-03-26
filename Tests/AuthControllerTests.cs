using System.Threading.Tasks;
using backend.Controllers;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class AuthControllerTests
{
    private readonly AuthController _controller;
    private readonly AppDbContext _context;
    private readonly Mock<JwtService> _mockJwtService;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        _context = new AppDbContext(options);
        _mockJwtService = new Mock<JwtService>();
        _controller = new AuthController(_context, _mockJwtService.Object);
    }

    [Fact]
    public async Task Register_CreatesNewUser()
    {
        var request = new AuthController.RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "securepassword"
        };

        var result = await _controller.Register(request) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal("Login", result.ActionName);
    }

    [Fact]
    public async Task Register_Fails_WhenUsernameAlreadyExists()
    {
        _context.Users.Add(new User { Username = "existinguser", Email = "test@example.com", Password = "pass" });
        await _context.SaveChangesAsync();

        var request = new AuthController.RegisterRequest
        {
            Username = "existinguser",
            Email = "newemail@example.com",
            Password = "securepassword"
        };

        var result = await _controller.Register(request) as ConflictObjectResult;
        Assert.NotNull(result);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_WhenEmailAlreadyExists()
    {
        _context.Users.Add(new User { Username = "user1", Email = "duplicate@example.com", Password = "pass" });
        await _context.SaveChangesAsync();

        var request = new AuthController.RegisterRequest
        {
            Username = "newuser",
            Email = "duplicate@example.com",
            Password = "securepassword"
        };

        var result = await _controller.Register(request) as ConflictObjectResult;
        Assert.NotNull(result);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_WhenInvalidEmail()
    {
        var request = new AuthController.RegisterRequest
        {
            Username = "user123",
            Email = "invalid-email",
            Password = "securepassword"
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");
        var result = await _controller.Register(request) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_WhenPasswordIsTooShort()
    {
        var request = new AuthController.RegisterRequest
        {
            Username = "user123",
            Email = "test@example.com",
            Password = "123"
        };

        _controller.ModelState.AddModelError("Password", "Password too short");
        var result = await _controller.Register(request) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_WhenXssInjection()
    {
        var request = new AuthController.RegisterRequest
        {
            Username = "<script>alert('hacked')</script>",
            Email = "xss@example.com",
            Password = "securepassword"
        };

        _controller.ModelState.AddModelError("Username", "Invalid characters");
        var result = await _controller.Register(request) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreCorrect()
    {
        var user = new User
        {
            Username = "validuser",
            Email = "validuser@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockJwtService.Setup(j => j.GenerateToken(user.Username)).Returns("fake-jwt-token");

        var request = new AuthController.LoginRequest { Username = "validuser", Password = "password" };
        var result = await _controller.Login(request) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("fake-jwt-token", result.Value.ToString());
    }

    [Fact]
    public async Task Login_Fails_WhenSqlInjection()
    {
        var request = new AuthController.LoginRequest { Username = "' OR '1'='1", Password = "password" };
        _controller.ModelState.AddModelError("Username", "Invalid characters");
        var result = await _controller.Login(request) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_AfterMultipleFailedAttempts()
    {
        var user = new User
        {
            Username = "secureuser",
            Email = "secure@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            var request = new AuthController.LoginRequest { Username = "secureuser", Password = "wrongpassword" };
            await _controller.Login(request);
        }

        var finalRequest = new AuthController.LoginRequest { Username = "secureuser", Password = "correctpassword" };
        var result = await _controller.Login(finalRequest) as UnauthorizedObjectResult;
        Assert.NotNull(result);
        Assert.Equal(401, result.StatusCode);
    }
}