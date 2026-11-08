using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthDemo.Data;

namespace AuthDemo.Controllers
{
    [ApiController]
    [Route("api/lookups")]
    public class LookupsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public LookupsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Feeds the Academic step: University -> Faculties -> Departments (cascading),
        // plus the Graduation Year options. Single source of truth for the frontend's
        // dropdowns instead of hardcoding them client-side.
        [HttpGet("academic")]
        public async Task<IActionResult> GetAcademicLookups()
        {
            var universities = await _db.Universities
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    Faculties = u.Faculties.Select(f => new
                    {
                        f.Id,
                        f.Name,
                        Departments = f.Departments.Select(d => new { d.Id, d.Name })
                    })
                })
                .ToListAsync();

            var currentYear = DateTime.UtcNow.Year;
            var graduationYears = Enumerable.Range(currentYear, 6).ToList(); // e.g. 2026..2031

            return Ok(new { universities, graduationYears });
        }
    }
}
