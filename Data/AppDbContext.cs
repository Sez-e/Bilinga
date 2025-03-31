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
        
        public DbSet<DictionaryEntry> Dictionary { get; set; }

        public DbSet<ChatHistory> ChatHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DictionaryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Word).IsRequired();
                entity.Property(e => e.Translation).IsRequired();
                entity.Property(e => e.Transcription).IsRequired();
            });

            modelBuilder.Entity<ChatHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
