using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]es")]
    public class BranchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/branches
        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.SysBranches
                .Include(b => b.SysArea) // join ke SysArea
                .Include(b => b.SysBranchType) // join ke BranchType
                .OrderBy(b => b.ID)
                .Select(b => new
                {
                    AreaName = b.SysArea != null ? b.SysArea.Name : null,
                    b.Ctrlbr,
                    b.Code,
                    b.Name,
                    BranchTypeName = b.SysBranchType != null ? b.SysBranchType.Name : null,
                    b.ppad_iplow,
                    b.ppad_iphigh,
                })
                .ToListAsync();

            if (branches == null || !branches.Any())
            {
                return NotFound(new { message = "Data branch tidak ditemukan." });
            }

            return Ok(branches);
        }

        // POST: api/branches
        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody] BranchCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            // Cek kode unik
            bool exists = await _context.SysBranches.AnyAsync(b => b.Code == request.Code);
            if (exists)
                return Conflict(new { message = $"Kode branch '{request.Code}' sudah digunakan." });

            // Validasi Area
            var area = await _context.SysAreas.FirstOrDefaultAsync(a => a.Code == request.Area);
            if (area == null)
                return BadRequest(new { message = $"Area dengan kode '{request.Area}' tidak ditemukan." });

            // Validasi BranchType
            var branchType = await _context.SysBranchTypes.FirstOrDefaultAsync(bt => bt.Code == request.BranchType);
            if (branchType == null)
                return BadRequest(new { message = $"BranchType dengan kode '{request.BranchType}' tidak ditemukan." });

            // Simpan Branch baru
            var newBranch = new SysBranch
            {
                Ctrlbr = request.Ctrlbr,
                Code = request.Code,
                Name = request.Name,
                Area = request.Area,
                Type = request.BranchType,
                ppad_iplow = request.ppad_iplow,
                ppad_iphigh = request.ppad_iphigh,
                ppad_seq = 0, // default
                CreateDate = DateTime.UtcNow,
                CreateBy = User?.Identity?.Name ?? "system",
                UpdateDate = DateTime.UtcNow,
                UpdateBy = User?.Identity?.Name ?? "system",
            };

            _context.SysBranches.Add(newBranch);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "SysBranches",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {newBranch.ID}",
                OldValues = "{}",
                NewValues = $"{{\"Code\":\"{newBranch.Code}\",\"Name\":\"{newBranch.Name}\",\"Area\":\"{area.Name}\",\"BranchType\":\"{branchType.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();
            // =================

            return CreatedAtAction(nameof(GetBranches), new { id = newBranch.ID }, new
            {
                newBranch.ID,
                newBranch.Ctrlbr,
                newBranch.Code,
                newBranch.Name,
                Area = area.Name,
                BranchType = branchType.Name,
                newBranch.ppad_iplow,
                newBranch.ppad_iphigh,
                newBranch.ppad_seq,
                newBranch.CreateDate,
                newBranch.CreateBy,
                newBranch.UpdateDate,
                newBranch.UpdateBy
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] BranchUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            var branch = await _context.SysBranches.FirstOrDefaultAsync(b => b.ID == id);
            if (branch == null)
                return NotFound(new { message = $"Branch dengan ID {id} tidak ditemukan." });

            // Cek kode unik selain dirinya sendiri
            bool exists = await _context.SysBranches.AnyAsync(b => b.Code == request.Code && b.ID != id);
            if (exists)
                return Conflict(new { message = $"Kode branch '{request.Code}' sudah digunakan oleh branch lain." });

            // Validasi Area
            var area = await _context.SysAreas.FirstOrDefaultAsync(a => a.Code == request.Area);
            if (area == null)
                return BadRequest(new { message = $"Area dengan kode '{request.Area}' tidak ditemukan." });

            // Validasi BranchType
            var branchType = await _context.SysBranchTypes.FirstOrDefaultAsync(bt => bt.Code == request.BranchType);
            if (branchType == null)
                return BadRequest(new { message = $"BranchType dengan kode '{request.BranchType}' tidak ditemukan." });

            // Simpan old values untuk audit
            var oldValues = $"{{\"Code\":\"{branch.Code}\",\"Name\":\"{branch.Name}\",\"Area\":\"{branch.Area}\",\"BranchType\":\"{branch.Type}\"}}";

            // Update fields
            branch.Ctrlbr = request.Ctrlbr;
            branch.Code = request.Code;
            branch.Name = request.Name;
            branch.Area = request.Area;
            branch.Type = request.BranchType;
            branch.ppad_iplow = request.ppad_iplow;
            branch.ppad_iphigh = request.ppad_iphigh;
            branch.UpdateDate = DateTime.UtcNow;
            branch.UpdateBy = User?.Identity?.Name ?? "system";

            _context.SysBranches.Update(branch);
            await _context.SaveChangesAsync();

            // Audit
            var audit = new Audit
            {
                TableName = "SysBranches",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {branch.ID}",
                OldValues = oldValues,
                NewValues = $"{{\"Code\":\"{branch.Code}\",\"Name\":\"{branch.Name}\",\"Area\":\"{area.Name}\",\"BranchType\":\"{branchType.Name}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Modified"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                branch.ID,
                branch.Ctrlbr,
                branch.Code,
                branch.Name,
                Area = area.Name,
                BranchType = branchType.Name,
                branch.ppad_iplow,
                branch.ppad_iphigh,
                branch.ppad_seq,
                branch.UpdateDate,
                branch.UpdateBy
            });
        }



    }
}
