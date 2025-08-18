using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class RegionalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegionalController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<IActionResult> GetRegionals()
        {
            var regionals = await _context.SysAreas
                                            .OrderBy(r => r.ID)
                                            .ToListAsync();

            if (regionals == null || !regionals.Any())
            {
                return NotFound(new { message = "Data regional tidak ditemukan." });
            }
            return Ok(regionals);
        }
    }
}