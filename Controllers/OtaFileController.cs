using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class OtaFileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OtaFileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/otafiles
        [HttpGet]
        public async Task<IActionResult> GetOtaFiles()
        {
            var result = await _context.OtaFiles
                .Select(o => new
                {
                    Id = o.OtaId,
                    OtaName = o.OtaDesc,
                    OtaFilename = o.OtaFilename,
                    RegisterDate = o.OtaCreateDate
                })
                .ToListAsync();

            return Ok(result);
        }

        // POST: api/otafiles
        [HttpPost]
        public async Task<IActionResult> CreateOtaFile([FromBody] OtaFileCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            if (string.IsNullOrEmpty(request.OtaDesc) || string.IsNullOrEmpty(request.OtaFilename))
                return BadRequest(new { message = "OtaDesc dan OtaFilename wajib diisi." });

            if (string.IsNullOrEmpty(request.OtaAttachment))
                return BadRequest(new { message = "Attachment wajib diisi." });

            // 🔎 Cek nama file unik
            bool exists = await _context.OtaFiles.AnyAsync(o => o.OtaFilename == request.OtaFilename);
            if (exists)
                return Conflict(new { message = $"Nama file '{request.OtaFilename}' sudah digunakan." });

            var otaFile = new OtaFile
            {
                OtaDesc = request.OtaDesc,
                OtaAttachment = request.OtaAttachment,
                OtaFilename = request.OtaFilename,
                OtaStatus = request.OtaStatus,
                OtaKey = Guid.NewGuid(),
                OtaCreateBy = User?.Identity?.Name ?? "system",
                OtaCreateDate = DateTime.UtcNow
            };

            _context.OtaFiles.Add(otaFile);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "OtaFiles",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {otaFile.OtaId}",
                OldValues = "{}",
                NewValues = $"{{\"OtaDesc\":\"{otaFile.OtaDesc}\",\"OtaFilename\":\"{otaFile.OtaFilename}\",\"OtaStatus\":\"{otaFile.OtaStatus}\"}}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();
            // =================

            return CreatedAtAction(nameof(GetOtaFiles), new { id = otaFile.OtaId }, otaFile);
        }

        // DELETE: api/otafiles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOtaFile(int id)
        {
            var otaFile = await _context.OtaFiles.FindAsync(id);
            if (otaFile == null)
                return NotFound(new { message = $"OtaFile dengan ID {id} tidak ditemukan." });

            // Simpan old values buat audit
            var oldValues = $"{{\"OtaDesc\":\"{otaFile.OtaDesc}\",\"OtaFilename\":\"{otaFile.OtaFilename}\",\"OtaStatus\":\"{otaFile.OtaStatus}\"}}";

            _context.OtaFiles.Remove(otaFile);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "OtaFiles",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {otaFile.OtaId}",
                OldValues = oldValues,
                NewValues = "{}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();
            // =================

            return Ok(new { message = $"OtaFile dengan ID {id} berhasil dihapus." });
        }

        // GET: api/otafiles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOtaFileById(int id)
        {
            var ota = await _context.OtaFiles
                .Where(o => o.OtaId == id)
                .Select(o => new
                {
                    Id = o.OtaId,
                    OtaDesc = o.OtaDesc,
                    OtaFilename = o.OtaFilename,
                    OtaAttachment = o.OtaAttachment,
                    OtaStatus = o.OtaStatus,
                    RegisterDate = o.OtaCreateDate
                })
                .FirstOrDefaultAsync();

            if (ota == null)
                return NotFound(new { message = $"OtaFile dengan ID {id} tidak ditemukan." });

            return Ok(ota);
        }


        // PUT: api/otafiles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOtaFile(int id, [FromBody] OtaFileUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            var otaFile = await _context.OtaFiles.FindAsync(id);
            if (otaFile == null)
                return NotFound(new { message = $"OtaFile dengan ID {id} tidak ditemukan." });

            // === Audit sebelum update (OldValues) ===
            var oldValues = new
            {
                otaFile.OtaDesc,
                otaFile.OtaFilename,
                otaFile.OtaAttachment,
                otaFile.OtaStatus
            };

            // === Update fields ===
            otaFile.OtaDesc = request.OtaDesc;
            otaFile.OtaAttachment = request.OtaAttachment;
            otaFile.OtaFilename = request.OtaFilename;
            otaFile.OtaStatus = request.OtaStatus;
            otaFile.OtaUpdateBy = User?.Identity?.Name ?? "system";
            otaFile.OtaUpdateDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // === Audit log setelah update (NewValues) ===
            var newValues = new
            {
                otaFile.OtaDesc,
                otaFile.OtaFilename,
                otaFile.OtaAttachment,
                otaFile.OtaStatus
            };

            var audit = new Audit
            {
                TableName = "OtaFiles",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {otaFile.OtaId}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
                NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Updated"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"OtaFile dengan ID {id} berhasil diperbarui." });
        }
    }
}