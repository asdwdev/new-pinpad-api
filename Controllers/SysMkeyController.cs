using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTO;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class SysMkeyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SysMkeyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/sysmkeys
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.SysMkeys
                .OrderByDescending(m => m.MkeyId)
                .ToListAsync();
            return Ok(items);
        }

        // GET: api/sysmkeys/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.SysMkeys.FindAsync(id);
            if (item == null)
                return NotFound(new { message = $"SysMkey with ID {id} not found." });
            return Ok(item);
        }

        // POST: api/sysmkeys
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SysMkeyCreateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            bool codeExists = await _context.SysMkeys.AnyAsync(x => x.MkeyCode == request.MkeyCode);
            if (codeExists)
                return Conflict(new { message = $"Kode '{request.MkeyCode}' sudah ada." });

            var entity = new SysMkey
            {
                MkeyCode = request.MkeyCode,
                MkeyNumber = request.MkeyNumber,
                MkeyDesc = request.MkeyDesc,
                MkeyCreateBy = User?.Identity?.Name ?? "system",
                MkeyCreateDate = DateTime.UtcNow,
                MkeyUpdateBy = User?.Identity?.Name ?? "system",
                MkeyUpdateDate = DateTime.UtcNow
            };

            _context.SysMkeys.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.MkeyId }, entity);
        }

        // PUT: api/sysmkeys/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SysMkeyUpdateRequest request)
        {
            var existing = await _context.SysMkeys.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"SysMkey dengan ID {id} tidak ditemukan." });

            // If code changed, ensure uniqueness
            if (!string.Equals(existing.MkeyCode, request.MkeyCode, StringComparison.OrdinalIgnoreCase))
            {
                bool codeExists = await _context.SysMkeys.AnyAsync(x => x.MkeyCode == request.MkeyCode && x.MkeyId != id);
                if (codeExists)
                    return Conflict(new { message = $"Kode '{request.MkeyCode}' sudah ada." });
            }

            existing.MkeyCode = request.MkeyCode;
            existing.MkeyNumber = request.MkeyNumber;
            existing.MkeyDesc = request.MkeyDesc;
            existing.MkeyUpdateBy = User?.Identity?.Name ?? "system";
            existing.MkeyUpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.SysMkeys.FindAsync(id);
            if (entity == null)
                return NotFound(new { message = "Data tidak ditemukan" });

            _context.SysMkeys.Remove(entity);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Data berhasil dihapus" });
        }
    }
}


