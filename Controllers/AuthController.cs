using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AuthDemo.Data;
using AuthDemo.DTOs;
using AuthDemo.Models;
using AuthDemo.Services;

namespace AuthDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AuthController(
            ApplicationDbContext db,
            ITokenService tokenService,
            IEmailService emailService,
            IConfiguration config)
        {
            _db = db;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        // Step 3 "Create Account" button -> creates an unverified user and emails a code.
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponseDto>> Register(RegisterDto dto)
        {
            if (!dto.AgreedToTerms)
                return BadRequest(new { message = "You must agree to the Terms of Service and Privacy Policy." });

            var emailTaken = await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (emailTaken)
                return Conflict(new { message = "An account with this email already exists." });

            var usernameTaken = await _db.Users.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (usernameTaken)
                return Conflict(new { message = "This username is already taken." });

            var lookupError = await ValidateAcademicSelectionAsync(dto.University, dto.Faculty, dto.Department);
            if (lookupError != null)
                return BadRequest(new { message = lookupError });

            if (dto.GraduationYear.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year;
                if (dto.GraduationYear < currentYear || dto.GraduationYear > currentYear + 10)
                    return BadRequest(new { message = "Graduation year is out of range." });
            }

            var code = GenerateVerificationCode();
            var expiryMinutes = int.Parse(_config["EmailVerification:CodeExpiryMinutes"] ?? "10");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Username = dto.Username,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                ProfilePictureUrl = dto.ProfilePictureUrl,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                University = dto.University,
                Faculty = dto.Faculty,
                Department = dto.Department,
                AcademicId = dto.AcademicId,
                GraduationYear = dto.GraduationYear,
                AgreedToTerms = true,
                AgreedToTermsAt = DateTime.UtcNow,
                IsEmailVerified = false,
                EmailVerificationCode = code,
                EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                EmailVerificationLastSentAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Two requests raced past the checks above; the unique indexes caught it.
                return Conflict(new { message = "An account with this email or username already exists." });
            }

            await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);

            return Ok(new RegisterResponseDto
            {
                Message = "Account created. Check your email for a verification code.",
                Email = user.Email
            });
        }

        // "Verify Email" screen -> confirms the 6-digit code and activates the account.
        [HttpPost("verify-email")]
        public async Task<ActionResult<AuthResponseDto>> VerifyEmail(VerifyEmailDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
                return NotFound(new { message = "No account found with this email." });

            if (user.IsEmailVerified)
                return BadRequest(new { message = "This account is already verified." });

            if (user.EmailVerificationCode == null ||
                user.EmailVerificationCodeExpiresAt == null ||
                user.EmailVerificationCodeExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "This code has expired. Request a new one." });

            var maxAttempts = int.Parse(_config["EmailVerification:MaxFailedAttempts"] ?? "5");
            if (user.EmailVerificationFailedAttempts >= maxAttempts)
                return StatusCode(429, new { message = "Too many wrong attempts. Request a new code." });

            if (user.EmailVerificationCode != dto.Code)
            {
                user.EmailVerificationFailedAttempts++;
                await _db.SaveChangesAsync();
                return BadRequest(new { message = "Invalid verification code." });
            }

            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiresAt = null;
            user.EmailVerificationFailedAttempts = 0;
            await _db.SaveChangesAsync();

            var (token, expiresAt) = _tokenService.CreateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = ToUserDto(user)
            });
        }

        // "Resend Code" link on the verification screen.
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode(ResendCodeDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
                return NotFound(new { message = "No account found with this email." });

            if (user.IsEmailVerified)
                return BadRequest(new { message = "This account is already verified." });

            // Matches the 60-second "Resend Code" timer on the verification screen.
            var cooldown = int.Parse(_config["EmailVerification:ResendCooldownSeconds"] ?? "60");
            if (user.EmailVerificationLastSentAt.HasValue)
            {
                var wait = cooldown - (int)(DateTime.UtcNow - user.EmailVerificationLastSentAt.Value).TotalSeconds;
                if (wait > 0)
                    return StatusCode(429, new { message = $"Please wait {wait} seconds before requesting a new code.", retryAfterSeconds = wait });
            }

            var expiryMinutes = int.Parse(_config["EmailVerification:CodeExpiryMinutes"] ?? "10");
            var code = GenerateVerificationCode();

            user.EmailVerificationCode = code;
            user.EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            user.EmailVerificationLastSentAt = DateTime.UtcNow;
            user.EmailVerificationFailedAttempts = 0;
            await _db.SaveChangesAsync();

            await _emailService.SendVerificationCodeAsync(user.Email, user.FirstName, code);

            return Ok(new { message = "A new verification code has been sent." });
        }

        // Login screen -> accepts either email or username, plus "remember me".
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == dto.EmailOrUsername.ToLower() ||
                u.Username.ToLower() == dto.EmailOrUsername.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email/username or password." });

            if (!user.IsEmailVerified)
                return StatusCode(403, new { message = "Please verify your email before signing in.", email = user.Email });

            var (token, expiresAt) = _tokenService.CreateToken(user, dto.RememberMe);

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = ToUserDto(user)
            });
        }

        // "Forgot password?" link on the login screen.
        // Always returns the same generic message, whether or not the email is
        // registered, so this endpoint can't be used to discover accounts.
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user != null)
            {
                var token = GenerateSecureToken();
                var expiryMinutes = int.Parse(_config["PasswordReset:TokenExpiryMinutes"] ?? "30");

                user.PasswordResetTokenHash = HashToken(token);
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
                await _db.SaveChangesAsync();

                var resetBaseUrl = _config["Frontend:ResetPasswordUrl"] ?? "http://localhost:3000/reset-password";
                var resetLink = $"{resetBaseUrl}?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetLink);
            }

            return Ok(new { message = "If an account exists with this email, a password reset link has been sent." });
        }

        // Reset-password page the link above points to.
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || user.PasswordResetTokenHash == null)
                return BadRequest(new { message = "Invalid or expired reset link." });

            if (user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
                return BadRequest(new { message = "This reset link has expired. Request a new one." });

            if (user.PasswordResetTokenHash != HashToken(dto.Token))
                return BadRequest(new { message = "Invalid or expired reset link." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Your password has been reset. You can now sign in." });
        }

        // Called by the frontend right after login (and on refresh) to load the
        // signed-in user's data instead of decoding the JWT client-side.
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized();

            var user = await _db.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            return Ok(ToUserDto(user));
        }

        private int? GetUserIdFromClaims()
        {
            var idClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        // Checks that University/Faculty/Department (if provided) actually exist and
        // are correctly nested (Faculty under that University, Department under that
        // Faculty) - matches the cascading dropdown on the Academic step.
        private async Task<string?> ValidateAcademicSelectionAsync(string? universityName, string? facultyName, string? departmentName)
        {
            if (string.IsNullOrWhiteSpace(universityName))
                return null; // nothing to validate

            var university = await _db.Universities
                .Include(u => u.Faculties)
                    .ThenInclude(f => f.Departments)
                .FirstOrDefaultAsync(u => u.Name == universityName);

            if (university == null)
                return "Unknown university.";

            if (string.IsNullOrWhiteSpace(facultyName))
                return null;

            var faculty = university.Faculties.FirstOrDefault(f => f.Name == facultyName);
            if (faculty == null)
                return "Unknown faculty for the selected university.";

            if (string.IsNullOrWhiteSpace(departmentName))
                return null;

            var department = faculty.Departments.FirstOrDefault(d => d.Name == departmentName);
            if (department == null)
                return "Unknown department for the selected faculty.";

            return null;
        }

        private static string GenerateVerificationCode()
        {
            return Random.Shared.Next(0, 1_000_000).ToString("D6");
        }

        // Cryptographically random, URL-safe token for password reset links.
        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        // Only the hash is ever stored/compared - the raw token never touches the DB.
        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }

        private static UserDto ToUserDto(User user) => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Role = user.Role.ToString(),
            University = user.University,
            Faculty = user.Faculty,
            Department = user.Department,
            AcademicId = user.AcademicId,
            GraduationYear = user.GraduationYear,
            IsEmailVerified = user.IsEmailVerified
        };
    }
}
