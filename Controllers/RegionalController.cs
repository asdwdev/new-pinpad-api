using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

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

        // GET: api/regionals
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

        // POST: api/regionals
        [HttpPost]
        public async Task<IActionResult> CreateRegional([FromBody] RegionalCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            // Cek kode unik
            bool exists = await _context.SysAreas.AnyAsync(r => r.Code == request.Code);
            if (exists)
                return Conflict(new { message = $"Kode area '{request.Code}' sudah digunakan." });

            var newRegional = new SysArea
            {
                Code = request.Code,
                Name = request.Name,
                CreateDate = DateTime.UtcNow,
                CreateBy = "system",
                UpdateDate = DateTime.UtcNow,
                UpdateBy = "system",
                Branches = new List<SysBranch>()
            };

            _context.SysAreas.Add(newRegional);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRegionals), new { id = newRegional.ID }, newRegional);
        }

    }
}