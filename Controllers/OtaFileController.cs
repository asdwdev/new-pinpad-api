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
        [RequestSizeLimit(5_000_000)] // max 5 MB
        public async Task<IActionResult> CreateOtaFile([FromForm] OtaFileCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            if (string.IsNullOrEmpty(request.OtaDesc) || string.IsNullOrEmpty(request.OtaFilename))
                return BadRequest(new { message = "OtaDesc dan OtaFilename wajib diisi." });

            if (request.OtaAttachment == null || request.OtaAttachment.Length == 0)
                return BadRequest(new { message = "Attachment wajib diupload." });

            // 🔎 Cek nama file unik
            bool exists = await _context.OtaFiles.AnyAsync(o => o.OtaFilename == request.OtaFilename);
            if (exists)
                return Conflict(new { message = $"Nama file '{request.OtaFilename}' sudah digunakan." });

            // === Simpan file fisik ===
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "otafiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // generate nama file unik biar ga tabrakan
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.OtaAttachment.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.OtaAttachment.CopyToAsync(stream);
            }

            var relativePath = Path.Combine("uploads/otafiles", uniqueFileName);

            // === Simpan DB ===
            var otaFile = new OtaFile
            {
                OtaDesc = request.OtaDesc,
                OtaAttachment = relativePath,   // simpan path file, bukan base64
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
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    otaFile.OtaDesc,
                    otaFile.OtaFilename,
                    otaFile.OtaStatus,
                    otaFile.OtaAttachment
                }),
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOtaFileById), new { id = otaFile.OtaId }, otaFile);
        }

        // DELETE: api/otafiles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOtaFile(int id)
        {
            var otaFile = await _context.OtaFiles.FindAsync(id);
            if (otaFile == null)
                return NotFound(new { message = $"OtaFile dengan ID {id} tidak ditemukan." });

            // Simpan old values buat audit
            var oldValues = new
            {
                otaFile.OtaDesc,
                otaFile.OtaFilename,
                otaFile.OtaAttachment,
                otaFile.OtaStatus
            };

            // === Hapus file fisik kalau ada ===
            if (!string.IsNullOrEmpty(otaFile.OtaAttachment))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", otaFile.OtaAttachment.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // === Hapus dari DB ===
            _context.OtaFiles.Remove(otaFile);
            await _context.SaveChangesAsync();

            // === Audit log ===
            var audit = new Audit
            {
                TableName = "OtaFiles",
                DateTimes = DateTime.UtcNow,
                KeyValues = $"ID: {otaFile.OtaId}",
                OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
                NewValues = "{}",
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"OtaFile dengan ID {id} berhasil dihapus" });
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
        [RequestSizeLimit(5_000_000)] // max 5 MB
        public async Task<IActionResult> UpdateOtaFile(int id, [FromForm] OtaFileUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Data tidak boleh kosong." });

            var otaFile = await _context.OtaFiles.FindAsync(id);
            if (otaFile == null)
                return NotFound(new { message = $"OtaFile dengan ID {id} tidak ditemukan." });

            // === Audit sebelum update ===
            var oldValues = new
            {
                otaFile.OtaDesc,
                otaFile.OtaFilename,
                otaFile.OtaAttachment,
                otaFile.OtaStatus
            };

            // === Update fields ===
            otaFile.OtaDesc = request.OtaDesc;
            otaFile.OtaFilename = request.OtaFilename;
            otaFile.OtaStatus = request.OtaStatus;
            otaFile.OtaUpdateBy = User?.Identity?.Name ?? "system";
            otaFile.OtaUpdateDate = DateTime.UtcNow;

            // === Kalau ada file baru, hapus file lama ===
            if (request.OtaAttachment != null && request.OtaAttachment.Length > 0)
            {
                // Hapus file lama (kalau ada)
                if (!string.IsNullOrEmpty(otaFile.OtaAttachment))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", otaFile.OtaAttachment.Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Upload file baru
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "otafiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.OtaAttachment.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.OtaAttachment.CopyToAsync(stream);
                }

                otaFile.OtaAttachment = Path.Combine("uploads/otafiles", uniqueFileName).Replace("\\", "/");
            }

            await _context.SaveChangesAsync();

            // === Audit log setelah update ===
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