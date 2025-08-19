using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;
using System.Text;
// using System.Drawing;
using OfficeOpenXml.Style;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;

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
                    b.ID,
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

        // GET: api/branches/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            var branch = await _context.SysBranches
                .Include(b => b.SysArea)
                .Include(b => b.SysBranchType)
                .Where(b => b.ID == id)
                .Select(b => new
                {
                    b.ID,
                    Area = b.SysArea != null ? b.SysArea.Code : null,
                    b.Ctrlbr,
                    b.Code,
                    b.Name,
                    BranchType = b.SysBranchType != null ? b.SysBranchType.Code : null,
                    b.ppad_iplow,
                    b.ppad_iphigh
                })
                .FirstOrDefaultAsync();

            if (branch == null)
                return NotFound(new { message = $"Branch dengan ID {id} tidak ditemukan." });

            return Ok(branch);
        }

        // GET: api/areas
        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var areas = await _context.SysAreas
                .Select(a => new { a.Code, a.Name })
                .OrderBy(a => a.Name)
                .ToListAsync();

            return Ok(areas);
        }

        // GET: api/branch-types
        [HttpGet("branch-types")]
        public async Task<IActionResult> GetBranchTypes()
        {
            var types = await _context.SysBranchTypes
                .Select(bt => new { bt.Code, bt.Name })
                .OrderBy(bt => bt.Name)
                .ToListAsync();

            return Ok(types);
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

        // PUT: api/branches/{id}
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.SysBranches
                                       .Include(b => b.Pinpads) // relasi child "outlet/pinpad"
                                       .FirstOrDefaultAsync(b => b.ID == id);

            if (branch == null)
                return NotFound(new { message = $"Branch dengan ID {id} tidak ditemukan." });

            // Cek apakah branch masih punya child (misal: pinpad/outlet)
            if (branch.Pinpads != null && branch.Pinpads.Any())
            {
                return BadRequest(new { message = "Gagal menghapus, masih ada child outlet/pinpad di bawah branch ini." });
            }

            // Simpan old values untuk audit
            var oldValues = $"{{\"Code\":\"{branch.Code}\",\"Name\":\"{branch.Name}\"}}";

            _context.SysBranches.Remove(branch);
            await _context.SaveChangesAsync();

            // Audit
            var audit = new Audit
            {
                TableName = "SysBranches",
                DateTimes = DateTime.Now,
                KeyValues = $"ID: {branch.ID}",
                OldValues = oldValues,
                NewValues = "{}", // kosong karena data dihapus
                Username = User?.Identity?.Name ?? "system",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Branch dengan ID {id} berhasil dihapus." });
        }



        [HttpGet("export")]
        public async Task<IActionResult> ExportBranches(
        string format = "csv",
        string? type = null,
        string? code = null,
        string? area = null,
        string? name = null)
        {
            try
            {
                // Simulasi proses export yang membutuhkan waktu
                await Task.Delay(2000); // Tunggu 2 detik untuk simulasi

                var query = from b in _context.SysBranches
                            select new BranchExportDto
                            {
                                KantorWilayah = b.SysArea != null ? b.SysArea.Name : "",
                                KodeCabangInduk = b.Ctrlbr ?? "",
                                CodeOutlet = b.Code ?? "",
                                NamaOutlet = b.Name ?? "",
                                Regional = b.Area ?? "",
                                KelasOutlet = b.Type ?? "",
                                IPLow = b.ppad_iplow ?? "",
                                IPHigh = b.ppad_iphigh ?? "",
                                ID = b.ID,
                                CreateDate = b.CreateDate,
                                CreateBy = b.CreateBy ?? "",
                                UpdateDate = b.UpdateDate,
                                UpdateBy = b.UpdateBy ?? ""
                            };
                // Debug: Log the initial query count
                var initialCount = await query.CountAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(b => b.KelasOutlet.ToLower() == type.ToLower());
                }

                if (!string.IsNullOrEmpty(code))
                {
                    query = query.Where(b => b.CodeOutlet.ToLower().Contains(code.ToLower()));
                }

                if (!string.IsNullOrEmpty(area))
                {
                    query = query.Where(b => b.Regional.ToLower().Contains(area.ToLower()));
                }

                if (!string.IsNullOrEmpty(name))
                {
                    query = query.Where(b => b.NamaOutlet.ToLower().Contains(name.ToLower()));
                }

                var branches = await query.ToListAsync();

                // Debug: Log filter details
                var debugInfo = new
                {
                    filtersApplied = new { type, code, area, name },
                    initialCount,
                    finalCount = branches.Count,
                    hasFilters = !string.IsNullOrEmpty(type) || !string.IsNullOrEmpty(code) ||
                                !string.IsNullOrEmpty(area) || !string.IsNullOrEmpty(name)
                };

                if (!branches.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tidak ada data branch yang ditemukan dengan filter yang diberikan.",
                        debug = debugInfo
                    });
                }

                switch (format.ToLower())
                {
                    case "csv":
                        var csvFile = GenerateBranchCsv(branches);
                        return File(csvFile, "text/csv", "BranchExport.csv");
                    case "xlsx":
                        var excelFile = GenerateBranchExcel(branches);
                        return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BranchExport.xlsx");
                    case "pdf":
                        var pdfFile = GenerateBranchPdf(branches);
                        return File(pdfFile, "application/pdf", "BranchExport.pdf");
                    default:
                        return BadRequest(new { success = false, message = "Format tidak didukung. Gunakan 'csv', 'xlsx', atau 'pdf'." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Export gagal.", error = ex.Message });
            }
        }

        // Method helper untuk generate CSV
        private byte[] GenerateBranchCsv(List<BranchExportDto> branches)
        {
            var csv = new StringBuilder();

            // Header CSV sesuai dengan gambar
            csv.AppendLine("Kantor Wilayah,Kode Cabang Induk,Code Outlet,Nama Outlet,Regional,Kelas Outlet,IP Low,IP High");

            foreach (var branch in branches)
            {
                csv.AppendLine($"{EscapeCsvValue(branch.KantorWilayah)}," +
                            $"{EscapeCsvValue(branch.KodeCabangInduk)}," +
                            $"{EscapeCsvValue(branch.CodeOutlet)}," +
                            $"{EscapeCsvValue(branch.NamaOutlet)}," +
                            $"{EscapeCsvValue(branch.Regional)}," +
                            $"{EscapeCsvValue(branch.KelasOutlet)}," +
                            $"{EscapeCsvValue(branch.IPLow)}," +
                            $"{EscapeCsvValue(branch.IPHigh)}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // Method helper untuk generate Excel
        // ...existing code...
        // Method helper untuk generate Excel (mirip dengan PinpadController)
        private byte[] GenerateBranchExcel(List<BranchExportDto> branches)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Branches");

                    var headers = new[] {
                        "Kantor Wilayah","Kode Cabang Induk","Code Outlet","Nama Outlet",
                        "Regional","Kelas Outlet","IP Low","IP High",
                        "ID","CreateDate","CreateBy","UpdateDate","UpdateBy"
                    };

                    for (int i = 0; i < headers.Length; i++)
                        ws.Cells[1, i + 1].Value = headers[i];

                    for (int r = 0; r < branches.Count; r++)
                    {
                        var b = branches[r];
                        int row = r + 2;
                        ws.Cells[row, 1].Value = b.KantorWilayah;
                        ws.Cells[row, 2].Value = b.KodeCabangInduk;
                        ws.Cells[row, 3].Value = b.CodeOutlet;
                        ws.Cells[row, 4].Value = b.NamaOutlet;
                        ws.Cells[row, 5].Value = b.Regional;
                        ws.Cells[row, 6].Value = b.KelasOutlet;
                        ws.Cells[row, 7].Value = b.IPLow;
                        ws.Cells[row, 8].Value = b.IPHigh;
                        ws.Cells[row, 9].Value = b.ID;
                        ws.Cells[row, 10].Value = b.CreateDate;
                        ws.Cells[row, 11].Value = b.CreateBy;
                        ws.Cells[row, 12].Value = b.UpdateDate;
                        ws.Cells[row, 13].Value = b.UpdateBy;
                    }

                    ws.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                }
            }
            catch
            {
                // Jika EPPlus error (mis. lisensi), fallback ke CSV yang aman
                return GenerateBranchCsvFallback(branches);
            }
        }

        // Fallback CSV jika Excel gagal
        private byte[] GenerateBranchCsvFallback(IEnumerable<BranchExportDto> branches)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Kantor Wilayah,Kode Cabang Induk,Code Outlet,Nama Outlet,Regional,Kelas Outlet,IP Low,IP High,ID,CreateDate,CreateBy,UpdateDate,UpdateBy");

            string Esc(object? o)
            {
                var s = o?.ToString() ?? "";
                if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                return s;
            }

            foreach (var b in branches)
            {
                sb.AppendLine(string.Join(",",
                    Esc(b.KantorWilayah),
                    Esc(b.KodeCabangInduk),
                    Esc(b.CodeOutlet),
                    Esc(b.NamaOutlet),
                    Esc(b.Regional),
                    Esc(b.KelasOutlet),
                    Esc(b.IPLow),
                    Esc(b.IPHigh),
                    Esc(b.ID),
                    Esc(b.CreateDate?.ToString("o")),
                    Esc(b.CreateBy),
                    Esc(b.UpdateDate?.ToString("o")),
                    Esc(b.UpdateBy)
                ));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        // ...existing code...
        // Method helper untuk generate PDF
        private byte[] GenerateBranchPdf(List<BranchExportDto> branches)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30)) // Landscape dengan margin yang lebih baik
                {
                    PdfWriter.GetInstance(doc, memoryStream);
                    doc.Open();

                    // Header dengan logo dan informasi perusahaan
                    var headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    // Logo/Company Info (kiri)
                    var companyCell = new PdfPCell(new Phrase("BRANCH MANAGEMENT SYSTEM", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64)))); // Dark gray color
                    companyCell.Border = Rectangle.NO_BORDER;
                    companyCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    companyCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    companyCell.PaddingBottom = 10;
                    headerTable.AddCell(companyCell);

                    // Export Info (kanan)
                    var exportInfo = new Paragraph();
                    exportInfo.Add(new Chunk("Generated: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                    exportInfo.Add(new Chunk("\nTotal Records: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(branches.Count.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                    var exportCell = new PdfPCell(exportInfo);
                    exportCell.Border = Rectangle.NO_BORDER;
                    exportCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    exportCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    headerTable.AddCell(exportCell);

                    doc.Add(headerTable);
                    doc.Add(new Paragraph(" ")); // Spacing

                    // Title dengan styling yang lebih menarik
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(255, 255, 255)); // White color
                    var title = new Paragraph("BRANCH DATA EXPORT", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;

                    var titleCell = new PdfPCell(title);
                    titleCell.BackgroundColor = new BaseColor(68, 114, 196); // Biru corporate
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingTop = 8;
                    titleCell.PaddingBottom = 8;
                    titleCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    var titleTable = new PdfPTable(1);
                    titleTable.WidthPercentage = 100;
                    titleTable.AddCell(titleCell);
                    doc.Add(titleTable);
                    doc.Add(new Paragraph(" ")); // Spacing

                    // Table dengan desain yang lebih baik
                    var table = new PdfPTable(8);
                    table.WidthPercentage = 100;
                    table.SpacingBefore = 10;
                    table.SpacingAfter = 10;

                    // Set column widths untuk optimasi layout
                    table.SetWidths(new float[] { 2.5f, 1.8f, 1.5f, 3f, 2f, 1.5f, 1.5f, 1.5f });

                    // Header styling
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255)); // White color
                    var headerBackground = new BaseColor(68, 114, 196);

                    // Headers dengan styling yang konsisten
                    var headers = new[] { "Kantor Wilayah", "Kode Cabang Induk", "Code Outlet", "Nama Outlet", "Regional", "Kelas Outlet", "IP Low", "IP High" };

                    foreach (var header in headers)
                    {
                        var headerCell = new PdfPCell(new Phrase(header, headerFont));
                        headerCell.BackgroundColor = headerBackground;
                        headerCell.Border = Rectangle.BOTTOM_BORDER;
                        headerCell.BorderColor = new BaseColor(255, 255, 255); // White color
                        headerCell.BorderWidthBottom = 2;
                        headerCell.PaddingTop = 6;
                        headerCell.PaddingBottom = 6;
                        headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        headerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        table.AddCell(headerCell);
                    }

                    // Data rows dengan alternating colors
                    var rowCount = 0;
                    var lightGray = new BaseColor(245, 245, 245);
                    var white = new BaseColor(255, 255, 255); // White color

                    foreach (var branch in branches)
                    {
                        // Alternating row colors
                        var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                        // Data cells
                        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                        var dataFontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);

                        // Kantor Wilayah
                        AddStyledCell(table, branch.KantorWilayah?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_LEFT);

                        // Kode Cabang Induk
                        AddStyledCell(table, branch.KodeCabangInduk?.ToString() ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);

                        // Code Outlet
                        AddStyledCell(table, branch.CodeOutlet?.ToString() ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);

                        // Nama Outlet
                        AddStyledCell(table, branch.NamaOutlet?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_LEFT);

                        // Regional
                        AddStyledCell(table, branch.Regional?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_LEFT);

                        // Kelas Outlet
                        AddStyledCell(table, branch.KelasOutlet?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_CENTER);

                        // IP Low
                        AddStyledCell(table, branch.IPLow?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_CENTER);

                        // IP High
                        AddStyledCell(table, branch.IPHigh?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_CENTER);

                        rowCount++;
                    }

                    doc.Add(table);

                    // Footer dengan informasi tambahan
                    var footerTable = new PdfPTable(1);
                    footerTable.WidthPercentage = 100;

                    var footerText = new Paragraph();
                    footerText.Add(new Chunk("Report generated by Branch Management System | ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128)))); // Gray color
                    footerText.Add(new Chunk("Page ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128)))); // Gray color
                    footerText.Add(new Chunk("1", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, new BaseColor(128, 128, 128)))); // Gray color
                    footerText.Add(new Chunk(" of 1", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128)))); // Gray color

                    var footerCell = new PdfPCell(footerText);
                    footerCell.Border = Rectangle.TOP_BORDER;
                    footerCell.BorderColor = new BaseColor(200, 200, 200); // Light gray color
                    footerCell.BorderWidthTop = 1;
                    footerCell.PaddingTop = 10;
                    footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    footerTable.AddCell(footerCell);

                    doc.Add(footerTable);
                    doc.Close();
                }

                return memoryStream.ToArray();
            }
        }

        // Helper method untuk menambahkan cell dengan styling yang konsisten
        private void AddStyledCell(PdfPTable table, string content, Font font, BaseColor backgroundColor, int alignment)
        {
            var cell = new PdfPCell(new Phrase(content, font));
            cell.BackgroundColor = backgroundColor;
            cell.Border = Rectangle.BOTTOM_BORDER | Rectangle.TOP_BORDER | Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
            cell.BorderColor = new BaseColor(200, 200, 200); // Light gray color
            cell.PaddingTop = 4;
            cell.PaddingBottom = 4;
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            table.AddCell(cell);
        }

        // Helper method untuk escape CSV values
        private string EscapeCsvValue(object value)
        {
            if (value == null) return "";

            string stringValue = value.ToString();
            if (stringValue.Contains(",") || stringValue.Contains("\"") || stringValue.Contains("\n"))
            {
                return "\"" + stringValue.Replace("\"", "\"\"") + "\"";
            }
            return stringValue;
        }

    }



}
