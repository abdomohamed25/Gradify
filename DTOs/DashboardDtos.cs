using System.ComponentModel.DataAnnotations;

namespace AuthDemo.DTOs
{
    // Same error shape the team already uses: { "message": "..." }
    public class ApiErrorDto
    {
        public string Message { get; set; } = string.Empty;
    }

    // ---------- Settings ----------

    public class SettingsDto
    {
        public string Theme { get; set; } = "Light";            // "Light" | "Dark"
        public bool NotificationsEnabled { get; set; } = true;
        public string Language { get; set; } = "en";            // "en" | "ar"
    }

    // Nullable + [Required] so a missing field is rejected instead of silently defaulting.
    public class UpdateSettingsDto
    {
        [Required]
        public string? Theme { get; set; }

        [Required]
        public bool? NotificationsEnabled { get; set; }

        [Required]
        public string? Language { get; set; }
    }

    public class SettingsUpdatedResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public SettingsDto Settings { get; set; } = new();
    }

    // ---------- Quick Actions ----------

    public class QuickActionDto
    {
        public string Key { get; set; } = string.Empty;     // stable id, e.g. "create_task"
        public string Label { get; set; } = string.Empty;   // text shown on the card
        public string Icon { get; set; } = string.Empty;    // icon name, frontend maps it to a real icon
    }

    public class QuickActionsResponseDto
    {
        public string Role { get; set; } = string.Empty;    // role used to filter the actions
        public List<QuickActionDto> Actions { get; set; } = new();
    }
}
