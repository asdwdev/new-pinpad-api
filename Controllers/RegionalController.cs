using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Attributes;
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

        // GET: api/regionals/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRegionalById(int id)
        {
            var regional = await _context.SysAreas
                                        .FirstOrDefaultAsync(r => r.ID == id);

            if (regional == null)
            {
                return NotFound(new { message = $"Regional dengan ID {id} tidak ditemukan." });
            }

            return Ok(regional);
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
                CreateBy = User?.Identity?.Name ?? "system",
                UpdateDate = DateTime.UtcNow,
                UpdateBy = User?.Identity?.Name ?? "system",
                Branches = new List<SysBranch>()
            };

            _context.SysAreas.Add(newRegional);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "SysAreas",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {newRegional.ID}",
                OldValues = "{}",
                NewValues = $"{{\"Code\":\"{newRegional.Code}\",\"Name\":\"{newRegional.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();
            // =================

            return CreatedAtAction(nameof(GetRegionals), new { id = newRegional.ID }, newRegional);
        }

        // PUT: api/regionals/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegional(int id, [FromBody] RegionalUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            var regional = await _context.SysAreas.FirstOrDefaultAsync(r => r.ID == id);

            if (regional == null)
                return NotFound(new { message = $"Regional dengan ID {id} tidak ditemukan." });

            // Cek apakah kode sudah dipakai regional lain
            bool exists = await _context.SysAreas.AnyAsync(r => r.Code == request.Code && r.ID != id);
            if (exists)
                return Conflict(new { message = $"Kode area '{request.Code}' sudah digunakan oleh regional lain." });

            // Simpan old values untuk audit
            var oldValues = $"{{\"Code\":\"{regional.Code}\",\"Name\":\"{regional.Name}\"}}";

            // Update fields
            regional.Code = request.Code;
            regional.Name = request.Name;
            regional.UpdateDate = DateTime.UtcNow;
            regional.UpdateBy = User?.Identity?.Name ?? "system";

            _context.SysAreas.Update(regional);
            await _context.SaveChangesAsync();

            // Simpan audit
            var audit = new Audit
            {
                TableName = "SysAreas",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {regional.ID}",
                OldValues = oldValues,
                NewValues = $"{{\"Code\":\"{regional.Code}\",\"Name\":\"{regional.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Modified"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(regional);
        }


        // DELETE: api/regionals/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegional(int id)
        {
            var regional = await _context.SysAreas
                                        .Include(r => r.Branches) // include relasi cabang
                                        .FirstOrDefaultAsync(r => r.ID == id);

            if (regional == null)
                return NotFound(new { message = $"Regional dengan ID {id} tidak ditemukan." });

            // Cek apakah ada cabang di bawah area ini
            if (regional.Branches != null && regional.Branches.Any())
            {
                return BadRequest(new { message = "Gagal menghapus, masih ada cabang di bawah regional ini." });
            }

            // Simpan old values untuk audit sebelum dihapus
            var oldValues = $"{{\"Code\":\"{regional.Code}\",\"Name\":\"{regional.Name}\"}}";

            _context.SysAreas.Remove(regional);
            await _context.SaveChangesAsync();

            // Simpan audit
            var audit = new Audit
            {
                TableName = "SysAreas",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {regional.ID}",
                OldValues = oldValues,
                NewValues = "{}", // karena data dihapus
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Regional dengan ID {id} berhasil dihapus." });
        }
    }
}