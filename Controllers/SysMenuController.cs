using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controllers
{
    [Route("api/[controller]s")]
    [ApiController]
    public class SysMenuController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SysMenuController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/sysmenus
        [HttpGet]
        public async Task<IActionResult> GetMenus()
        {
            var menus = await _context.SysMenus
                .Where(m => m.ParentId == null) // ambil root menus
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Icon,
                    m.Urls,
                    Children = m.Children.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Icon,
                        c.Urls
                    })
                })
                .ToListAsync();

            return Ok(menus);
        }
    }

}