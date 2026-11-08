using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Models
{
    public enum UserRole
    {
        Student,
        Supervisor
    }

    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ProfilePictureUrl { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        // Academic step fields. Kept as plain text (not foreign keys) so the API
        // contract doesn't change - validated against the lookup tables in the
        // controller instead. See LookupsController / AcademicLookup.cs.
        [MaxLength(150)]
        public string? University { get; set; }

        [MaxLength(100)]
        public string? Faculty { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? AcademicId { get; set; }

        public int? GraduationYear { get; set; }

        // Terms of Service / Privacy Policy checkbox on the Security step
        public bool AgreedToTerms { get; set; } = false;
        public DateTime? AgreedToTermsAt { get; set; }

        // Email verification
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiresAt { get; set; }
        public DateTime? EmailVerificationLastSentAt { get; set; }
        public int EmailVerificationFailedAttempts { get; set; } = 0;

        // Password reset ("Forgot password?" on the login screen)
        // Only the hash of the reset token is stored, never the raw token.
        public string? PasswordResetTokenHash { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
