using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [Route("api/[controller]s")]
    [ApiController]
    public class LevelMenuController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LevelMenuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/levelmenus/3
        [HttpGet("{levelId}")]
        public async Task<IActionResult> GetMenusByLevel(int levelId)
        {
            // ambil semua menu granted
            var grantedMenus = await _context.LinkLevelMenus
                .Where(lm => lm.LevelId == levelId)
                .Select(lm => lm.SysMenu)
                .ToListAsync();

            // ambil parent-parent yang mungkin belum ke-grant
            var parentIds = grantedMenus
                .Where(m => m.ParentId.HasValue)
                .Select(m => m.ParentId.Value)
                .Distinct()
                .ToList();

            var parents = await _context.SysMenus
                .Where(m => parentIds.Contains(m.Id))
                .ToListAsync();

            var allMenus = grantedMenus.Union(parents).Distinct().ToList();

            // return flat list (frontend build tree)
            var result = allMenus.Select(m => new
            {
                m.Id,
                m.Name,
                m.Icon,
                m.Urls,
                m.ParentId
            });

            return Ok(result);
        }


        // POST: api/levelmenus
        [HttpPost]
        public async Task<IActionResult> AssignMenu([FromBody] LinkLevelMenu request)
        {
            if (request == null) return BadRequest();

            request.CreatedDate = DateTime.Now;
            request.GrantedBy = "SUP-ADM"; // bisa ganti sesuai user login

            _context.LinkLevelMenus.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Menu assigned successfully" });
        }

        // DELETE: api/levelmenus/3/5
        [HttpDelete("{levelId}/{menuId}")]
        public async Task<IActionResult> RemoveMenu(int levelId, int menuId)
        {
            var entity = await _context.LinkLevelMenus
                .FirstOrDefaultAsync(lm => lm.LevelId == levelId && lm.MenuId == menuId);

            if (entity == null)
                return NotFound(new { message = "Link not found" });

            _context.LinkLevelMenus.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Menu removed successfully" });
        }
    }

}