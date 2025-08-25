using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class SysResponseCodeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SysResponseCodeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/sysresponsecodes
        [HttpGet]
        public async Task<IActionResult> GetSysResponseCodes()
        {
            var responseCodes = await _context.SysResponseCodes.ToListAsync();

            if (responseCodes == null || !responseCodes.Any())
            {
                return NotFound(new { message = "No response code found." });
            }

            return Ok(responseCodes);
        }

        // GET: api/sysresponsecodes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSysResponseCode(int id)
        {
            var responseCode = await _context.SysResponseCodes.FindAsync(id);

            if (responseCode == null)
            {
                return NotFound(new { message = $"Response code with ID {id} not found." });
            }

            return Ok(responseCode);
        }

        // POST: api/sysresponsecodes
        [HttpPost]
        public async Task<IActionResult> CreateSysResponseCode([FromBody] SysResponseCodeCreateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔎 Cek apakah RescodeCode sudah ada
            var exists = await _context.SysResponseCodes
                .AnyAsync(r => r.RescodeCode == request.RescodeCode);
            if (exists)
                return Conflict(new { message = $"Kode response '{request.RescodeCode}' sudah digunakan." });

            var entity = new SysResponseCode
            {
                RescodeType = request.RescodeType,
                RescodeCode = request.RescodeCode,
                RescodeDesc = request.RescodeDesc,
                RescodeCreateBy = User?.Identity?.Name ?? "system",
                RescodeCreateDate = DateTime.UtcNow,
                RescodeUpdateBy = User?.Identity?.Name ?? "system",
                RescodeUpdateDate = DateTime.UtcNow
            };

            _context.SysResponseCodes.Add(entity);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "SysResponseCodes",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {entity.RescodeId}",
                OldValues = "{}",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    entity.RescodeType,
                    entity.RescodeCode,
                    entity.RescodeDesc
                }),
                Username = entity.RescodeCreateBy,
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSysResponseCode), new { id = entity.RescodeId }, entity);
        }


        // PUT: api/sysresponsecodes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSysResponseCode(int id, [FromBody] SysResponseCodeUpdateRequest request)
        {
            var existing = await _context.SysResponseCodes.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Response code dengan ID {id} tidak ditemukan." });

            var oldValues = new
            {
                existing.RescodeType,
                existing.RescodeCode,
                existing.RescodeDesc
            };

            existing.RescodeType = request.RescodeType;
            existing.RescodeDesc = request.RescodeDesc;
            existing.RescodeUpdateBy = User?.Identity?.Name ?? "system";
            existing.RescodeUpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var newValues = new
            {
                existing.RescodeType,
                existing.RescodeCode,
                existing.RescodeDesc
            };

            var audit = new Audit
            {
                TableName = "SysResponseCodes",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {existing.RescodeId}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
                NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Updated"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(existing);
        }


        // DELETE: api/sysresponsecodes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSysResponseCode(int id)
        {
            var responseCode = await _context.SysResponseCodes
                                             .FirstOrDefaultAsync(r => r.RescodeId == id);

            if (responseCode == null)
                return NotFound(new { message = $"Response code dengan ID {id} tidak ditemukan." });

            // Cek apakah ada Pinpad yang masih pakai rescode ini
            bool inUse = await _context.Pinpads
                .AnyAsync(p => p.PpadStatusRepair == responseCode.RescodeCode);

            if (inUse)
            {
                return BadRequest(new { message = $"Gagal menghapus, Response Code '{responseCode.RescodeCode}' masih digunakan oleh Pinpad." });
            }

            // Simpan old values untuk audit
            var oldValues = new
            {
                responseCode.RescodeType,
                responseCode.RescodeCode,
                responseCode.RescodeDesc
            };

            _context.SysResponseCodes.Remove(responseCode);
            await _context.SaveChangesAsync();

            // Simpan audit
            var audit = new Audit
            {
                TableName = "SysResponseCodes",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {responseCode.RescodeId}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
                NewValues = "{}", // karena data dihapus
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Response code '{responseCode.RescodeCode}' berhasil dihapus." });
        }
    }
}
