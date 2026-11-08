using Microsoft.EntityFrameworkCore;
using AuthDemo.Models;

namespace AuthDemo.Data
{
    // A separate, tiny DbContext that only knows about User_Settings.
    // Because of that it cannot clash with the team's existing DbContext / migrations.
    public class SettingsDbContext : DbContext
    {
        public SettingsDbContext(DbContextOptions<SettingsDbContext> options) : base(options) { }

        public DbSet<UserSettings> UserSettings => Set<UserSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.ToTable("User_Settings");
                entity.HasIndex(s => s.UserId).IsUnique();   // exactly one row per user
                entity.Property(s => s.Theme).HasConversion<string>().HasMaxLength(10);
            });
        }
    }
}
