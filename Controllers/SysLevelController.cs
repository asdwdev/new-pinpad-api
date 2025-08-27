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
            var levels = await _context.SysLevels
                .Where(l => l.Name.ToLower() != "super admin") // skip Super Admin
                .ToListAsync();

            if (levels == null || !levels.Any())
                return NotFound(new { message = "No system levels found." });

            return Ok(levels);
        }

        // --- GET SYSLEVEL BY ID ---
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSysLevel(int id)
        {
            var level = await _context.SysLevels
                .Include(l => l.LinkLevelMenus)
                .ThenInclude(llm => llm.SysMenu)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (level == null)
                return NotFound(new { message = $"System level with ID {id} not found." });

            var response = new
            {
                level.Id,
                level.Name,
                level.Description,
                MenuIds = level.LinkLevelMenus.Select(x => x.MenuId).ToList()
            };

            return Ok(response);
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

            // 1. Buat SysLevel baru
            var level = new SysLevel
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = now,
                CreatedBy = "SUP-ADM"
            };

            _context.SysLevels.Add(level);
            await _context.SaveChangesAsync();

            // 2. Insert ke LinkLevelMenu
            if (request.MenuIds != null && request.MenuIds.Any())
            {
                var links = request.MenuIds.Select(menuId => new LinkLevelMenu
                {
                    LevelId = level.Id,
                    MenuId = menuId,
                    CreatedDate = now,
                    GrantedBy = "SUP-ADM"
                });

                _context.LinkLevelMenus.AddRange(links);
                await _context.SaveChangesAsync();
            }

            // 3. Audit pakai DTO (hindari cycle)
            var audit = new Audit
            {
                TableName = "SysLevels",
                DateTimes = now,
                KeyValues = $"ID: {level.Id}",
                OldValues = "{}",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    level.Id,
                    level.Name,
                    level.Description,
                    level.CreatedAt,
                    level.CreatedBy
                }),
                Username = "SUP-ADM",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            // 4. Response DTO
            var response = new SysLevelResponse
            {
                Id = level.Id,
                Name = level.Name,
                Description = level.Description,
                MenuIds = request.MenuIds ?? new List<int>()
            };

            return CreatedAtAction(nameof(GetSysLevel), new { id = level.Id }, response);
        }

        // --- UPDATE SYSLEVEL ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSysLevel(int id, [FromBody] SysLevelUpdateRequest request)
        {
            var level = await _context.SysLevels
                .Include(l => l.LinkLevelMenus)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (level == null)
                return NotFound(new { message = $"System level with ID {id} not found." });

            // update field dasar
            level.Name = request.Name;
            level.Description = request.Description;
            level.UpdatedAt = DateTime.Now;
            level.UpdatedBy = "SUP-ADM";

            // update link menus
            var existingLinks = _context.LinkLevelMenus.Where(lm => lm.LevelId == id);
            _context.LinkLevelMenus.RemoveRange(existingLinks);

            if (request.MenuIds != null && request.MenuIds.Any())
            {
                var newLinks = request.MenuIds.Select(menuId => new LinkLevelMenu
                {
                    LevelId = id,
                    MenuId = menuId,
                    CreatedDate = DateTime.Now,
                    GrantedBy = "SUP-ADM"
                });

                _context.LinkLevelMenus.AddRange(newLinks);
            }

            await _context.SaveChangesAsync();

            // 🔥 return DTO tanpa cycle
            var response = new SysLevelResponse
            {
                Id = level.Id,
                Name = level.Name,
                Description = level.Description,
                MenuIds = request.MenuIds ?? new List<int>()
            };
            return Ok(response);
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
