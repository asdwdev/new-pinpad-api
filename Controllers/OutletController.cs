using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class OutletController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OutletController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/outlets
        [HttpGet]
        public async Task<IActionResult> GetOutlets()
        {
            var outlets = await _context.SysBranchTypes
                                        .OrderByDescending(o => o.Id)
                                        .Select(o => new OutletDto
                                        {
                                            Id = o.Id,
                                            Code = o.Code,
                                            Name = o.Name
                                        })
                                        .ToListAsync();

            if (outlets == null || !outlets.Any())
            {
                return NotFound(new { Message = "Data outlet tidak ditemukan." });
            }

            return Ok(outlets);
        }

        // GET: api/outlets/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOutletById(int id)
        {
            var outlet = await _context.SysBranchTypes
                                        .FirstOrDefaultAsync(o => o.Id == id);

            if (outlet == null)
            {
                return NotFound(new { message = $"Outlet dengan ID {id} tidak ditemukan." });
            }

            return Ok(outlet);
        }

        // POST: api/outlets
        [HttpPost]
        public async Task<IActionResult> CreateOutlet([FromBody] OutletCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            // Cek kode unik
            bool exists = await _context.SysBranchTypes.AnyAsync(o => o.Code == request.Code);
            if (exists)
                return Conflict(new { message = $"Kode outlet '{request.Code}' sudah digunakan." });

            var newOutletType = new SysBranchType
            {
                Code = request.Code,
                Name = request.Name,
                CreateDate = DateTime.UtcNow,
                CreateBy = User?.Identity?.Name ?? "system",
                UpdateDate = DateTime.UtcNow,
                UpdateBy = User?.Identity?.Name ?? "system",
                Branches = new List<SysBranch>()
            };

            _context.SysBranchTypes.Add(newOutletType);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "SysBranchTypes",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {newOutletType.Id}",
                OldValues = "{}",
                NewValues = $"{{\"Code\":\"{newOutletType.Code}\",\"Name\":\"{newOutletType.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();
            // =================

            return CreatedAtAction(nameof(GetOutlets), new { id = newOutletType.Id }, newOutletType);
        }

        // PUT: api/outlets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOutlet(int id, [FromBody] OutletUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            var outletType = await _context.SysBranchTypes.FirstOrDefaultAsync(o => o.Id == id);

            if (outletType == null)
                return NotFound(new { message = $"Outlet dengan ID {id} tidak ditemukan." });

            // Cek apakah kode sudah dipakai outlet lain
            bool exists = await _context.SysBranchTypes.AnyAsync(o => o.Code == request.Code && o.Id != id);
            if (exists)
                return Conflict(new { message = $"Kode outlet '{request.Code}' sudah digunakan oleh outlet lain." });

            // Simpan old values untuk audit
            var oldValues = $"{{\"Code\":\"{outletType.Code}\",\"Name\":\"{outletType.Name}\"}}";

            // Update fields
            outletType.Code = request.Code;
            outletType.Name = request.Name;
            outletType.UpdateDate = DateTime.UtcNow;
            outletType.UpdateBy = User?.Identity?.Name ?? "system";

            _context.SysBranchTypes.Update(outletType);
            await _context.SaveChangesAsync();

            // Simpan audit
            var audit = new Audit
            {
                TableName = "SysBranchTypes",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {outletType.Id}",
                OldValues = oldValues,
                NewValues = $"{{\"Code\":\"{outletType.Code}\",\"Name\":\"{outletType.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Modified"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(outletType);
        }

        // DELETE: api/outlets/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOutlet(int id)
        {
            var outletType = await _context.SysBranchTypes
                                           .Include(o => o.Branches) // include relasi cabang
                                           .FirstOrDefaultAsync(o => o.Id == id);

            if (outletType == null)
                return NotFound(new { message = $"Outlet dengan ID {id} tidak ditemukan." });

            // Cek apakah ada cabang di bawah outlet type ini
            if (outletType.Branches != null && outletType.Branches.Any())
            {
                return BadRequest(new { message = "Gagal menghapus, masih ada cabang di bawah outlet ini." });
            }

            // Simpan old values untuk audit sebelum dihapus
            var oldValues = $"{{\"Code\":\"{outletType.Code}\",\"Name\":\"{outletType.Name}\"}}";

            _context.SysBranchTypes.Remove(outletType);
            await _context.SaveChangesAsync();

            // Simpan audit
            var audit = new Audit
            {
                TableName = "SysBranchTypes",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {outletType.Id}",
                OldValues = oldValues,
                NewValues = "{}", // karena data dihapus
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Outlet dengan ID {id} berhasil dihapus." });
        }
    }
}