using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace AuthDemo.Controllers
{
    // Called from the Personal step, before the account exists yet - so it's not
    // behind auth. Returns a URL to include as ProfilePictureUrl in the register call.
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("profile-picture")]
        [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest(new { message = "Only JPG, PNG, or WEBP images are allowed." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "profile-pictures");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { url = $"/uploads/profile-pictures/{fileName}" });
        }
    }
}
