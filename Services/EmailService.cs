using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace AuthDemo.Services
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string toEmail, string firstName, string code);
        Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerificationCodeAsync(string toEmail, string firstName, string code)
        {
            var expiryMinutes = _config["EmailVerification:CodeExpiryMinutes"] ?? "10";

            await SendAsync(
                toEmail,
                subject: "Verify your Gradify account",
                body:
                    $"Hi {firstName},\n\n" +
                    $"Your Gradify verification code is: {code}\n" +
                    $"This code expires in {expiryMinutes} minutes.\n\n" +
                    "If you didn't request this, you can ignore this email.",
                devLogLabel: $"Verification code for {toEmail}: {code} (expires in {expiryMinutes} min)"
            );
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string firstName, string resetLink)
        {
            var expiryMinutes = _config["PasswordReset:TokenExpiryMinutes"] ?? "30";

            await SendAsync(
                toEmail,
                subject: "Reset your Gradify password",
                body:
                    $"Hi {firstName},\n\n" +
                    $"Click the link below to reset your password:\n{resetLink}\n" +
                    $"This link expires in {expiryMinutes} minutes.\n\n" +
                    "If you didn't request this, you can safely ignore this email.",
                devLogLabel: $"Password reset link for {toEmail}: {resetLink}"
            );
        }

        private async Task SendAsync(string toEmail, string subject, string body, string devLogLabel)
        {
            var smtp = _config.GetSection("Smtp");
            var host = smtp["Host"];

            // Dev fallback: if no SMTP host is configured, just log instead of failing.
            // Lets you test register/verify/forgot-password locally with no mail setup.
            if (string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine($"[DEV] {devLogLabel}");
                return;
            }

            using var client = new SmtpClient(host, int.Parse(smtp["Port"] ?? "587"))
            {
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtp["FromEmail"] ?? "no-reply@gradify.com", smtp["FromName"] ?? "Gradify"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}
