using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthDemo.DTOs;
using AuthDemo.Extensions;
using AuthDemo.Services;

namespace AuthDemo.Controllers
{
    // ---------------- SETTINGS ----------------
    // [Authorize] = "User must be authenticated" (SRS 8.5). No valid token -> 401 automatically.
    [ApiController]
    [Route("api/settings")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settings;

        public SettingsController(ISettingsService settings)
        {
            _settings = settings;
        }

        // GET /api/settings  -> load current settings when the Settings page opens
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new ApiErrorDto { Message = "Could not read the user id from the token." });

            return Ok(await _settings.GetAsync(userId));
        }

        // PUT /api/settings  -> "Save Settings" button
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSettingsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new ApiErrorDto { Message = "Could not read the user id from the token." });

            try
            {
                var (success, error, result) = await _settings.UpdateAsync(userId, dto);

                if (!success)
                    return BadRequest(new ApiErrorDto { Message = error! });

                // The response carries the saved settings so the frontend can apply them immediately (FR-03).
                return Ok(new SettingsUpdatedResponseDto
                {
                    Message = "Settings updated successfully.",
                    Settings = result!
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiErrorDto { Message = "Failed to save settings." });
            }
        }

        // POST /api/settings/reset  -> "Reset Settings" button (back to defaults)
        [HttpPost("reset")]
        public async Task<IActionResult> Reset()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new ApiErrorDto { Message = "Could not read the user id from the token." });

            try
            {
                var result = await _settings.ResetAsync(userId);

                return Ok(new SettingsUpdatedResponseDto
                {
                    Message = "Settings updated successfully.",
                    Settings = result
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiErrorDto { Message = "Failed to save settings." });
            }
        }
    }

    // ---------------- QUICK ACTIONS ----------------
    [ApiController]
    [Route("api/quick-actions")]
    [Authorize]
    public class QuickActionsController : ControllerBase
    {
        private readonly IQuickActionsService _quickActions;

        public QuickActionsController(IQuickActionsService quickActions)
        {
            _quickActions = quickActions;
        }

        // GET /api/quick-actions -> the cards the Dashboard should show for THIS user's role
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Unauthorized(new ApiErrorDto { Message = "Could not read the user id from the token." });

            return Ok(await _quickActions.GetForUserAsync(userId, User.GetRoles()));
        }
    }
}
