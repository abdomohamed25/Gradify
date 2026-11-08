using Microsoft.EntityFrameworkCore;
using AuthDemo.Models;

namespace AuthDemo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Store the enum as its string name ("Student" / "Supervisor") instead of an int,
            // so the raw DB rows stay readable.
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.University)
                .WithMany(u => u.Faculties)
                .HasForeignKey(f => f.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Faculty)
                .WithMany(f => f.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data matching the Academic step's dropdowns exactly.
            modelBuilder.Entity<University>().HasData(
                new University { Id = 1, Name = "Egyptian E-Learning University (EELU)" }
            );

            modelBuilder.Entity<Faculty>().HasData(
                new Faculty { Id = 1, UniversityId = 1, Name = "Computers and Information Technology" },
                new Faculty { Id = 2, UniversityId = 1, Name = "Business Administration" },
                new Faculty { Id = 3, UniversityId = 1, Name = "Engineering" }
            );

            modelBuilder.Entity<Department>().HasData(
                // Computers and Information Technology (FacultyId = 1)
                new Department { Id = 1, FacultyId = 1, Name = "Computer Science" },
                new Department { Id = 2, FacultyId = 1, Name = "Software Engineering" },
                new Department { Id = 3, FacultyId = 1, Name = "Information Systems" },
                new Department { Id = 4, FacultyId = 1, Name = "Information Technology" },
                // Business Administration (FacultyId = 2)
                new Department { Id = 5, FacultyId = 2, Name = "Accounting" },
                new Department { Id = 6, FacultyId = 2, Name = "Marketing" },
                new Department { Id = 7, FacultyId = 2, Name = "Management Information Systems" },
                // Engineering (FacultyId = 3)
                new Department { Id = 8, FacultyId = 3, Name = "Computer Engineering" },
                new Department { Id = 9, FacultyId = 3, Name = "Electronics & Communications" }
            );
        }
    }
}
