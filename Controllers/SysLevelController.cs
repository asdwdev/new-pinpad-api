using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;
using NewPinpadApi.DTOs;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class SysLevelController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SysLevelController(AppDbContext context)
        {
            _context = context;
        }

        // --- GET ALL SYSLEVELS ---
        [HttpGet]
        public async Task<IActionResult> GetSysLevels()
        {
            var levels = await _context.SysLevels.ToListAsync();
            if (levels == null || !levels.Any())
                return NotFound(new { message = "No system levels found." });

            return Ok(levels);
        }

        // --- GET SYSLEVEL BY ID ---
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSysLevel(int id)
        {
            var level = await _context.SysLevels.FindAsync(id);
            if (level == null)
                return NotFound(new { message = $"System level with ID {id} not found." });

            return Ok(level);
        }

        // --- CREATE SYSLEVEL ---
        [HttpPost]
        public async Task<IActionResult> CreateSysLevel([FromBody] SysLevelCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid system level data." });

            var exists = await _context.SysLevels.AnyAsync(l => l.Name == request.Name);
            if (exists)
                return Conflict(new { message = "System level with the same name already exists." });

            var now = DateTime.Now;

            var level = new SysLevel
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = now,
                CreatedBy = "SUP-ADM"
            };

            _context.SysLevels.Add(level);
            await _context.SaveChangesAsync();

            var audit = new Audit
            {
                TableName = "SysLevels",
                DateTimes = now,
                KeyValues = $"ID: {level.Id}",
                OldValues = "{}",
                NewValues = System.Text.Json.JsonSerializer.Serialize(level),
                Username = "SUP-ADM",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSysLevel), new { id = level.Id }, level);
        }

        // --- UPDATE SYSLEVEL ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSysLevel(int id, [FromBody] SysLevelUpdateRequest request)
        {
            var level = await _context.SysLevels.FindAsync(id);
            if (level == null)
                return NotFound(new { message = $"System level with ID {id} not found." });

            var nameExists = await _context.SysLevels
                .AnyAsync(l => l.Name == request.Name && l.Id != id);
            if (nameExists)
                return Conflict(new { message = "System level with the same name already exists." });

            var oldValues = System.Text.Json.JsonSerializer.Serialize(level);
            var now = DateTime.Now;

            level.Name = request.Name;
            level.Description = request.Description;
            level.UpdatedAt = now;
            level.UpdatedBy = "SUP-ADM";

            await _context.SaveChangesAsync();

            var newValues = System.Text.Json.JsonSerializer.Serialize(level);

            var audit = new Audit
            {
                TableName = "SysLevels",
                DateTimes = now,
                KeyValues = $"ID: {level.Id}",
                OldValues = oldValues,
                NewValues = newValues,
                Username = "SUP-ADM",
                ActionType = "Updated"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(level);
        }

        // --- DELETE SYSLEVEL ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSysLevel(int id)
        {
            var level = await _context.SysLevels.FindAsync(id);
            if (level == null)
                return NotFound(new { message = $"System level with ID {id} not found." });

            var oldValues = System.Text.Json.JsonSerializer.Serialize(level);
            var now = DateTime.Now;

            _context.SysLevels.Remove(level);
            await _context.SaveChangesAsync();

            var audit = new Audit
            {
                TableName = "SysLevels",
                DateTimes = now,
                KeyValues = $"ID: {level.Id}",
                OldValues = oldValues,
                NewValues = "{}",
                Username = "SUP-ADM",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"System level with ID {id} deleted successfully." });
        }

        // --- GET SYSLEVEL NAMES (FOR DROPDOWN) ---
        [HttpGet("dropdown")]
        public async Task<IActionResult> GetSysLevelDropdown()
        {
            var levels = await _context.SysLevels
                .Select(l => new
                {
                    id = l.Id,
                    name = l.Name
                })
                .ToListAsync();

            if (levels == null || !levels.Any())
                return NotFound(new { message = "No system levels found." });

            return Ok(levels);
        }
    }
}
