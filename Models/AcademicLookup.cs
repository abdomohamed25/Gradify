using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Models
{
    public class University
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public List<Faculty> Faculties { get; set; } = new();
    }

    public class Faculty
    {
        public int Id { get; set; }

        public int UniversityId { get; set; }
        public University? University { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public List<Department> Departments { get; set; } = new();
    }

    public class Department
    {
        public int Id { get; set; }

        public int FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
