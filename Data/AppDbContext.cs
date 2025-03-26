using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        
        public DbSet<Note> Notes { get; set; }
        
        public DbSet<Article> Articles { get; set; }
        
        public DbSet<Dictionary> Dictionary { get; set; }
    }
}
