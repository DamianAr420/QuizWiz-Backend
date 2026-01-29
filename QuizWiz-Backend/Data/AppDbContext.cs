using Microsoft.EntityFrameworkCore;
using QuizWiz_Backend.Classes;
using System.Text.Json;

namespace QuizWiz_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Quiz> Quizzes => Set<Quiz>();
        public DbSet<Question> Questions => Set<Question>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.DisplayName)
                .IsUnique();

            modelBuilder.Entity<Question>()
                .Property(q => q.Distractors)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>()
                );

            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne()
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}