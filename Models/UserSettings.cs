using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Models
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    // Maps to the "User_Settings" table in the SRS (Module 02, section 8.10). One row per user.
    //
    // UserId is a plain string on purpose: it holds whatever id the team's login puts in the token
    // (int, Guid or text), and there is no foreign key to the Users table, so this table works
    // without knowing anything about how Module 01 stores users.
    public class UserSettings
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        public AppTheme Theme { get; set; } = AppTheme.Light;

        public bool NotificationsEnabled { get; set; } = true;

        [Required, MaxLength(5)]
        public string Language { get; set; } = "en";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
