using Xunit;
using Microsoft.EntityFrameworkCore;
using backend.Controllers;
using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

public class NotesControllerTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    [Fact]
    public void Get_ReturnsAllNotes()
    {
        var context = GetDbContext("Get_ReturnsAllNotes");
        context.Notes.Add(new Note { Title = "Test", Content = "Hello" });
        context.SaveChanges();

        var controller = new NotesController(context);
        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<IEnumerable<Note>>(ok.Value);
        Assert.Single(notes);
    }

    [Fact]
    public void GetById_ExistingNote_ReturnsNote()
    {
        var context = GetDbContext("GetById_ExistingNote");
        var note = new Note { Title = "Note 1", Content = "Content" };
        context.Notes.Add(note);
        context.SaveChanges();

        var controller = new NotesController(context);
        var result = controller.GetById(note.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var found = Assert.IsType<Note>(ok.Value);
        Assert.Equal("Note 1", found.Title);
    }

    [Fact]
    public void GetById_NonExistingNote_ReturnsNotFound()
    {
        var controller = new NotesController(GetDbContext("GetById_NonExistingNote"));
        var result = controller.GetById(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_ValidNote_ReturnsCreatedNote()
    {
        var context = GetDbContext("Create_ValidNote");
        var controller = new NotesController(context);

        var createNote = new CreateNote { Title = "New Note", Content = "Some content" };
        var result = controller.Create(createNote);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var note = Assert.IsType<Note>(created.Value);
        Assert.Equal("New Note", note.Title);
        Assert.True(note.Id > 0);
    }

    [Fact]
    public void Create_EmptyTitle_ReturnsBadRequest()
    {
        var controller = new NotesController(GetDbContext("Create_EmptyTitle"));
        var result = controller.Create(new CreateNote { Title = "", Content = "Missing title" });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Title is required.", bad.Value);
    }

    [Fact]
    public void Update_ExistingNote_ChangesValues()
    {
        var context = GetDbContext("Update_ExistingNote");
        var note = new Note { Title = "Old", Content = "Text" };
        context.Notes.Add(note);
        context.SaveChanges();

        var controller = new NotesController(context);
        var update = new UpdateNote { Title = "Updated Title", Content = "Updated Content" };
        var result = controller.Update(note.Id, update);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Note>(ok.Value);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated Content", updated.Content);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public void Update_NonExistingNote_ReturnsNotFound()
    {
        var controller = new NotesController(GetDbContext("Update_NonExistingNote"));
        var update = new UpdateNote { Title = "Nothing" };
        var result = controller.Update(123, update);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Delete_ExistingNote_RemovesIt()
    {
        var context = GetDbContext("Delete_ExistingNote");
        var note = new Note { Title = "Delete Me", Content = "Soon gone" };
        context.Notes.Add(note);
        context.SaveChanges();

        var controller = new NotesController(context);
        var result = controller.Delete(note.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var msg = Assert.IsAssignableFrom<Dictionary<string, string>>(ok.Value);
        Assert.Equal("Note successfully deleted.", msg["message"]);

        Assert.Empty(context.Notes.ToList());
    }

    [Fact]
    public void Delete_NonExistingNote_ReturnsNotFound()
    {
        var controller = new NotesController(GetDbContext("Delete_NonExistingNote"));
        var result = controller.Delete(555);

        Assert.IsType<NotFoundResult>(result);
    }
}