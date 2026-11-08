using Microsoft.EntityFrameworkCore;
using AuthDemo.Data;
using AuthDemo.DTOs;
using AuthDemo.Models;

namespace AuthDemo.Services
{
    public interface ISettingsService
    {
        Task<SettingsDto> GetAsync(string userId);
        Task<(bool Success, string? Error, SettingsDto? Result)> UpdateAsync(string userId, UpdateSettingsDto dto);
        Task<SettingsDto> ResetAsync(string userId);
    }

    public class SettingsService : ISettingsService
    {
        // Add a language here and it becomes valid everywhere.
        private static readonly string[] SupportedLanguages = { "en", "ar" };

        private readonly SettingsDbContext _db;

        public SettingsService(SettingsDbContext db)
        {
            _db = db;
        }

        public async Task<SettingsDto> GetAsync(string userId)
        {
            var settings = await GetOrCreateAsync(userId);
            return ToDto(settings);
        }

        public async Task<(bool Success, string? Error, SettingsDto? Result)> UpdateAsync(
            string userId, UpdateSettingsDto dto)
        {
            if (!Enum.TryParse<AppTheme>(dto.Theme, ignoreCase: true, out var theme)
                || !Enum.IsDefined(typeof(AppTheme), theme))
                return (false, "Theme must be 'Light' or 'Dark'.", null);

            var language = dto.Language!.Trim().ToLowerInvariant();
            if (!SupportedLanguages.Contains(language))
                return (false, $"Language must be one of: {string.Join(", ", SupportedLanguages)}.", null);

            var settings = await GetOrCreateAsync(userId);

            settings.Theme = theme;
            settings.NotificationsEnabled = dto.NotificationsEnabled!.Value;
            settings.Language = language;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return (true, null, ToDto(settings));
        }

        // "Reset Settings" button: put everything back to defaults.
        public async Task<SettingsDto> ResetAsync(string userId)
        {
            var settings = await GetOrCreateAsync(userId);

            settings.Theme = AppTheme.Light;
            settings.NotificationsEnabled = true;
            settings.Language = "en";
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ToDto(settings);
        }

        // First time a user opens Settings there is no row yet, so we create one with defaults.
        private async Task<UserSettings> GetOrCreateAsync(string userId)
        {
            var existing = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existing != null) return existing;

            var created = new UserSettings { UserId = userId };
            _db.UserSettings.Add(created);

            try
            {
                await _db.SaveChangesAsync();
                return created;
            }
            catch (DbUpdateException)
            {
                // Two requests created the row at the same moment (unique index on UserId).
                // Drop ours and read the one that won.
                _db.Entry(created).State = EntityState.Detached;
                return await _db.UserSettings.FirstAsync(s => s.UserId == userId);
            }
        }

        private static SettingsDto ToDto(UserSettings s) => new()
        {
            Theme = s.Theme.ToString(),
            NotificationsEnabled = s.NotificationsEnabled,
            Language = s.Language
        };
    }
}
