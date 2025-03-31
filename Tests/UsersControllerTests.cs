using System.Collections.Generic;
using System.Linq;
using backend.Controllers;
using backend.Data;
using backend.Models;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class UsersControllerTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly UsersController _controller;
    private readonly List<User> _users;

    public UsersControllerTests()
    {
        _mockContext = new Mock<AppDbContext>();
        _controller = new UsersController(_mockContext.Object);

        _users = new List<User>
        {
            new User { Id = 1, Email = "user1@example.com", Username = "User1", Password = "pass1", Score = 10 },
            new User { Id = 2, Email = "user2@example.com", Username = "User2", Password = "pass2", Score = 20 }
        };
    }

    [Fact]
    public void Get_ReturnsAllUsers()
    {
        _mockContext.Setup(c => c.Users.ToList()).Returns(_users);
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var users = Assert.IsType<List<User>>(result.Value);
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public void Get_ReturnsEmptyList_WhenNoUsersExist()
    {
        _mockContext.Setup(c => c.Users.ToList()).Returns(new List<User>());
        var result = _controller.Get() as OkObjectResult;
        Assert.NotNull(result);
        var users = Assert.IsType<List<User>>(result.Value);
        Assert.Empty(users);
    }

    [Fact]
    public void GetById_ReturnsUser_IfExists()
    {
        _mockContext.Setup(c => c.Users.Find(1)).Returns(_users[0]);
        var result = _controller.GetById(1) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var user = Assert.IsType<User>(result.Value);
        Assert.Equal("User1", user.Username);
    }

    [Fact]
    public void GetById_ReturnsNotFound_IfUserDoesNotExist()
    {
        _mockContext.Setup(c => c.Users.Find(3)).Returns((User)null);
        var result = _controller.GetById(3) as NotFoundResult;
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Update_UserExists_UpdatesUser()
    {
        var existingUser = _users[0];
        var updatedUser = new UpdateUser { 
            Email = "new@example.com", 
            Username = "NewUser", 
            Score = 50, 
            Password = "newpass" 
        };
        _mockContext.Setup(c => c.Users.FindAsync(It.IsAny<int>())).ReturnsAsync(existingUser);
        
        // Имитируем Claims для авторизованного пользователя
        var claims = new List<Claim> { new Claim("id", "1") };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Update(updatedUser) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("NewUser", existingUser.Username);
        Assert.Equal("new@example.com", existingUser.Email);
        Assert.Equal(50, existingUser.Score);
    }

    [Fact]
    public async Task Update_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _mockContext.Setup(c => c.Users.FindAsync(It.IsAny<int>())).ReturnsAsync((User)null);
        var updatedUser = new UpdateUser { 
            Email = "new@example.com", 
            Username = "NewUser", 
            Score = 50, 
            Password = "newpass" 
        };

        // Имитируем Claims для авторизованного пользователя
        var claims = new List<Claim> { new Claim("id", "3") };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Update(updatedUser) as NotFoundResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Update_UserExists_WithNullPassword_DoesNotChangePassword()
    {
        // Arrange
        var existingUser = _users[0];
        var updatedUser = new UpdateUser { 
            Email = "user1@updated.com", 
            Username = "UpdatedUser", 
            Score = 99, 
            Password = null 
        };
        _mockContext.Setup(c => c.Users.FindAsync(It.IsAny<int>())).ReturnsAsync(existingUser);
        
        // Имитируем Claims для авторизованного пользователя
        var claims = new List<Claim> { new Claim("id", "1") };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var oldPassword = existingUser.Password;
        var result = await _controller.Update(updatedUser) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(oldPassword, existingUser.Password);
    }

    [Fact]
    public async Task Update_UserExists_WithEmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = _users[0];
        var updatedUser = new UpdateUser { 
            Email = "test@example.com", 
            Username = "", 
            Score = 30, 
            Password = "newpass" 
        };
        _mockContext.Setup(c => c.Users.FindAsync(It.IsAny<int>())).ReturnsAsync(existingUser);
        
        // Имитируем Claims для авторизованного пользователя
        var claims = new List<Claim> { new Claim("id", "1") };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.Update(updatedUser) as BadRequestResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public void Delete_UserExists_RemovesUser()
    {
        var user = _users[0];
        _mockContext.Setup(c => c.Users.Find(1)).Returns(user);
        var result = _controller.Delete(1) as OkObjectResult;
        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Delete_UserDoesNotExist_ReturnsNotFound()
    {
        _mockContext.Setup(c => c.Users.Find(3)).Returns((User)null);
        var result = _controller.Delete(3) as NotFoundResult;
        Assert.NotNull(result);
        Assert.Equal(404, result.StatusCode);
    }
}

