using System.ComponentModel.DataAnnotations;

namespace backend.Models;
public class User
{
    public int Id { get; set; }

    [StringLength(20)] public required string Username { get; set; }

    public required string Email { get; set; }

    public required string Password { get; set; }

    [StringLength(12)] public string? PhoneNumber { get; set; }

    public int Score { get; set; }

    public bool IsTemporaryPassword { get; set; }
}