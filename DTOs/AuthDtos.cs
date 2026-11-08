using System.ComponentModel.DataAnnotations;
using AuthDemo.Models;

namespace AuthDemo.DTOs
{
    // Submitted once, after all 3 frontend steps (Personal / Academic / Security) are filled in.
    public class RegisterDto
    {
        // Step 1 - Personal
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        // Not visible in the screenshots, assumed to live on step 1 since it's required
        // for account creation and email verification.
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Enter a valid Egyptian phone number (e.g. 01012345678).")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Returned by POST /api/upload/profile-picture, optional
        public string? ProfilePictureUrl { get; set; }

        // Step 2 - Academic
        [Required]
        public UserRole Role { get; set; }

        [MaxLength(150)]
        public string? University { get; set; }

        [MaxLength(100)]
        public string? Faculty { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? AcademicId { get; set; }

        public int? GraduationYear { get; set; }

        // Step 3 - Security
        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        // "I agree to the Terms of Service and Privacy Policy" checkbox.
        // Checked explicitly in the controller (must be true) rather than via
        // annotation, so the error message is clear.
        public bool AgreedToTerms { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class VerifyEmailDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }

    public class ResendCodeDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword))]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? University { get; set; }
        public string? Faculty { get; set; }
        public string? Department { get; set; }
        public string? AcademicId { get; set; }
        public int? GraduationYear { get; set; }
        public bool IsEmailVerified { get; set; }
    }

    public class RegisterResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }
}
