using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OutletsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OutletsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/outlets
        [HttpGet]
        public async Task<IActionResult> GetOutlets()
        {
            // Ambil semua outlet dari database
            var outlets = await _context.SysBranchTypes
                                        .OrderByDescending(o => o.Id)
                                        .Select(o => new OutletDto
                                        {
                                            Id = o.Id,
                                            Code = o.Code,
                                            Name = o.Name
                                        })
                                        .ToListAsync();

            // Kalau data kosong
            if (outlets == null || !outlets.Any())
            {
                return NotFound(new { message = "No outlet data found." });
            }

            return Ok(outlets);
        }

        // GET: api/outlets/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOutletById(int id)
        {
            // Cari outlet berdasarkan ID
            var outlet = await _context.SysBranchTypes
                                       .FirstOrDefaultAsync(o => o.Id == id);

            // Kalau tidak ditemukan
            if (outlet == null)
            {
                return NotFound(new { message = $"Outlet with ID {id} was not found." });
            }

            return Ok(outlet);
        }

        // POST: api/outlets
        [HttpPost]
        public async Task<IActionResult> CreateOutlet([FromBody] OutletCreateRequest request)
        {
            // Validasi request kosong
            if (request == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            // Cek kode outlet unik
            bool exists = await _context.SysBranchTypes.AnyAsync(o => o.Code == request.Code);
            if (exists)
                return Conflict(new { message = $"The outlet code '{request.Code}' is already in use." });

            // Buat outlet baru
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

            // Simpan audit log
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

            return CreatedAtAction(nameof(GetOutletById), new { id = newOutletType.Id }, newOutletType);
        }

        // PUT: api/outlets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOutlet(int id, [FromBody] OutletUpdateRequest request)
        {
            // Validasi request kosong
            if (request == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            // Cari outlet berdasarkan ID
            var outletType = await _context.SysBranchTypes.FirstOrDefaultAsync(o => o.Id == id);
            if (outletType == null)
                return NotFound(new { message = $"Outlet with ID {id} was not found." });

            // Cek apakah kode sudah dipakai outlet lain
            bool exists = await _context.SysBranchTypes.AnyAsync(o => o.Code == request.Code && o.Id != id);
            if (exists)
                return Conflict(new { message = $"The outlet code '{request.Code}' is already in use by another outlet." });

            // Simpan nilai lama untuk audit
            var oldValues = $"{{\"Code\":\"{outletType.Code}\",\"Name\":\"{outletType.Name}\"}}";

            // Update field outlet
            outletType.Code = request.Code;
            outletType.Name = request.Name;
            outletType.UpdateDate = DateTime.UtcNow;
            outletType.UpdateBy = User?.Identity?.Name ?? "system";

            _context.SysBranchTypes.Update(outletType);
            await _context.SaveChangesAsync();

            // Simpan audit log
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
            // Cari outlet berdasarkan ID
            var outletType = await _context.SysBranchTypes
                                           .Include(o => o.Branches) // include relasi cabang
                                           .FirstOrDefaultAsync(o => o.Id == id);

            if (outletType == null)
                return NotFound(new { message = $"Outlet with ID {id} was not found." });

            // Cek apakah masih ada cabang di bawah outlet
            if (outletType.Branches != null && outletType.Branches.Any())
            {
                return BadRequest(new { message = "Failed to delete, there are still branches under this outlet." });
            }

            // Simpan nilai lama untuk audit sebelum hapus
            var oldValues = $"{{\"Code\":\"{outletType.Code}\",\"Name\":\"{outletType.Name}\"}}";

            _context.SysBranchTypes.Remove(outletType);
            await _context.SaveChangesAsync();

            // Simpan audit log
            var audit = new Audit
            {
                TableName = "SysBranchTypes",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {outletType.Id}",
                OldValues = oldValues,
                NewValues = "{}", // data dihapus
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Outlet with ID {id} has been successfully deleted." });
        }
    }
}