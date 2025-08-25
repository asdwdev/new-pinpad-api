using iTextSharp.text.pdf;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;
using iTextSharp.text;

namespace NewPinpadApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/audit
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Audit>>> GetAudits(
            [FromQuery] string? username,
            [FromQuery] string? actionType,
            [FromQuery] string? keyValues,
            [FromQuery] string? oldValues,
            [FromQuery] string? newValues,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate
        )
        {
            var query = _context.Audits.AsQueryable();

            if (!string.IsNullOrEmpty(username))
                query = query.Where(a => a.Username.Contains(username));

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(a => a.ActionType == actionType);

            if (!string.IsNullOrEmpty(keyValues))
                query = query.Where(a => a.KeyValues.Contains(keyValues));

            if (!string.IsNullOrEmpty(oldValues))
                query = query.Where(a => a.OldValues.Contains(oldValues));

            if (!string.IsNullOrEmpty(newValues))
                query = query.Where(a => a.NewValues.Contains(newValues));

            if (startDate.HasValue)
                query = query.Where(a => a.DateTimes >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.DateTimes <= endDate.Value);

            var logs = await query
                .OrderByDescending(a => a.DateTimes)
                .ToListAsync();

            if (!logs.Any())
                return Ok(new { message = "Data tidak ditemukan" });

            return Ok(logs);
        }

        // GET: api/audit/pinpads
        [HttpGet("pinpads")]
        public async Task<ActionResult<IEnumerable<Audit>>> GetPinpadAudits(
            [FromQuery] string? username,
            [FromQuery] string? actionType,
            [FromQuery] string? keyValues,
            [FromQuery] string? oldValues,
            [FromQuery] string? newValues,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate
        )
        {
            var query = _context.Audits.AsQueryable();

            // filter khusus Pinpads
            query = query.Where(a => a.TableName == "Pinpads");

            if (!string.IsNullOrEmpty(username))
                query = query.Where(a => a.Username.Contains(username));

            if (!string.IsNullOrEmpty(actionType))
                query = query.Where(a => a.ActionType == actionType);

            if (!string.IsNullOrEmpty(keyValues))
                query = query.Where(a => a.KeyValues.Contains(keyValues));

            if (!string.IsNullOrEmpty(oldValues))
                query = query.Where(a => a.OldValues.Contains(oldValues));

            if (!string.IsNullOrEmpty(newValues))
                query = query.Where(a => a.NewValues.Contains(newValues));

            if (startDate.HasValue)
                query = query.Where(a => a.DateTimes >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.DateTimes <= endDate.Value);

            var logs = await query
                .OrderByDescending(a => a.DateTimes)
                .ToListAsync();

            if (!logs.Any())
                return Ok(new { message = "Data tidak ditemukan" });

            return Ok(logs);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAudits(
    string format = "xlsx",
    [FromQuery] string? tableName = null,   // ✅ Tambahan
    [FromQuery] string? username = null,
    [FromQuery] string? actionType = null,
    [FromQuery] string? keyValues = null,
    [FromQuery] string? oldValues = null,
    [FromQuery] string? newValues = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null
)
        {
            try
            {
                var query = _context.Audits.AsQueryable();

                // === Apply filters ===
                if (!string.IsNullOrEmpty(tableName))
                    query = query.Where(a => a.TableName == tableName);

                if (!string.IsNullOrEmpty(username))
                    query = query.Where(a => a.Username.Contains(username));

                if (!string.IsNullOrEmpty(actionType))
                    query = query.Where(a => a.ActionType == actionType);

                if (!string.IsNullOrEmpty(keyValues))
                    query = query.Where(a => a.KeyValues.Contains(keyValues));

                if (!string.IsNullOrEmpty(oldValues))
                    query = query.Where(a => a.OldValues.Contains(oldValues));

                if (!string.IsNullOrEmpty(newValues))
                    query = query.Where(a => a.NewValues.Contains(newValues));

                if (startDate.HasValue)
                    query = query.Where(a => a.DateTimes >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.DateTimes <= endDate.Value);

                var audits = await query
                    .OrderByDescending(a => a.DateTimes)
                    .ToListAsync();

                if (!audits.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tidak ada data audit log yang ditemukan dengan filter yang diberikan.",
                        filtersApplied = new { tableName, username, actionType, keyValues, oldValues, newValues, startDate, endDate }
                    });
                }

                // === Simpan log Export ke Audit ===
                var audit = new Audit
                {
                    TableName = "Audit",
                    DateTimes = DateTime.Now,
                    KeyValues = "Export",
                    OldValues = "{}",
                    NewValues = $"{{\"ExportFormat\":\"{format}\",\"ResultCount\":{audits.Count},\"TableName\":\"{tableName ?? "All"}\"}}",
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Export"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                // === Generate file sesuai format ===
                switch (format.ToLower())
                {
                    case "xlsx":
                        var excelFile = GenerateAuditExcel(audits);
                        return File(
                            excelFile,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"AuditExport_{tableName ?? "All"}.xlsx"
                        );

                    case "csv":
                        var csvFile = GenerateAuditCsv(audits);
                        return File(
                            csvFile,
                            "text/csv",
                            $"AuditExport_{tableName ?? "All"}.csv"
                        );

                    case "pdf":
                        var pdfFile = GenerateAuditPdf(audits);
                        return File(
                            pdfFile,
                            "application/pdf",
                            $"AuditExport_{tableName ?? "All"}.pdf"
                        );

                    default:
                        return BadRequest(new { success = false, message = "Format tidak didukung. Pilih 'xlsx', 'csv', atau 'pdf'." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Export gagal.", error = ex.Message });
            }
        }


        private byte[] GenerateAuditExcel(List<Audit> audits)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Audit Logs");

            // Header sesuai field audit log
            string[] headers = {
        "ID", "Table Name", "Date Time", "Username",
        "Action Type", "Key Values", "Old Values", "New Values"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;

            int r = 2;
            foreach (var a in audits)
            {
                ws.Cell(r, 1).Value = a.ID;
                ws.Cell(r, 2).Value = a.TableName ?? "";
                ws.Cell(r, 3).Value = a.DateTimes;
                ws.Cell(r, 3).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
                ws.Cell(r, 4).Value = a.Username ?? "";
                ws.Cell(r, 5).Value = a.ActionType ?? "";
                ws.Cell(r, 6).Value = a.KeyValues ?? "";
                ws.Cell(r, 7).Value = a.OldValues ?? "";
                ws.Cell(r, 8).Value = a.NewValues ?? "";
                r++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private byte[] GenerateAuditCsv(List<Audit> audits)
        {
            using var sw = new StringWriter();

            // Header
            var csvHeader = "ID,Table Name,Date Time,Username,Action Type,Key Values,Old Values,New Values";
            sw.WriteLine(csvHeader);

            foreach (var a in audits)
            {
                var csvRow = string.Join(",",
                    a.ID,
                    EscapeCsv(a.TableName),
                    a.DateTimes.ToString("dd-MM-yyyy HH:mm:ss"),
                    EscapeCsv(a.Username),
                    EscapeCsv(a.ActionType),
                    EscapeCsv(a.KeyValues),
                    EscapeCsv(a.OldValues),
                    EscapeCsv(a.NewValues)
                );

                sw.WriteLine(csvRow);
            }

            return System.Text.Encoding.UTF8.GetBytes(sw.ToString());
        }

        // Fungsi helper buat escape value CSV (biar aman kalau ada koma / tanda kutip)
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Kalau ada koma atau kutip, bungkus pakai tanda kutip ganda
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        private byte[] GenerateAuditPdf(List<Audit> audits)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30)) // Landscape
                {
                    PdfWriter.GetInstance(doc, memoryStream);
                    doc.Open();

                    // === Header ===
                    var headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    // Kiri (Title Sistem)
                    var companyCell = new PdfPCell(new Phrase(
                        "AUDIT LOG MANAGEMENT SYSTEM",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64))
                    ));
                    companyCell.Border = Rectangle.NO_BORDER;
                    companyCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    companyCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    companyCell.PaddingBottom = 10;
                    headerTable.AddCell(companyCell);

                    // Kanan (info export)
                    var exportInfo = new Paragraph();
                    exportInfo.Add(new Chunk("Generated: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                    exportInfo.Add(new Chunk("\nTotal Records: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(audits.Count.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                    var exportCell = new PdfPCell(exportInfo);
                    exportCell.Border = Rectangle.NO_BORDER;
                    exportCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    exportCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    headerTable.AddCell(exportCell);

                    doc.Add(headerTable);
                    doc.Add(new Paragraph(" ")); // spacing

                    // === Title Section ===
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
                    var title = new Paragraph("AUDIT LOG EXPORT", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };

                    var titleCell = new PdfPCell(title);
                    titleCell.BackgroundColor = new BaseColor(68, 114, 196);
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingTop = 8;
                    titleCell.PaddingBottom = 8;
                    titleCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    var titleTable = new PdfPTable(1);
                    titleTable.WidthPercentage = 100;
                    titleTable.AddCell(titleCell);
                    doc.Add(titleTable);
                    doc.Add(new Paragraph(" "));

                    // === Table ===
                    var table = new PdfPTable(7);
                    table.WidthPercentage = 100;
                    table.SpacingBefore = 10;
                    table.SpacingAfter = 10;

                    table.SetWidths(new float[] { 1.5f, 1.2f, 1.2f, 2.5f, 2.5f, 2.5f, 1.8f });

                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                    var headerBg = new BaseColor(68, 114, 196);

                    string[] headers = { "Username", "Action", "Table", "Key Values", "Old Values", "New Values", "Date" };

                    foreach (var h in headers)
                    {
                        var cell = new PdfPCell(new Phrase(h, headerFont));
                        cell.BackgroundColor = headerBg;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.PaddingTop = 6;
                        cell.PaddingBottom = 6;
                        table.AddCell(cell);
                    }

                    // === Data Rows ===
                    var rowCount = 0;
                    var lightGray = new BaseColor(245, 245, 245);
                    var white = BaseColor.WHITE;
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                    foreach (var a in audits)
                    {
                        var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                        AddStyledCell(table, a.Username ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                        AddStyledCell(table, a.ActionType ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, a.TableName ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, a.KeyValues ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                        AddStyledCell(table, a.OldValues ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                        AddStyledCell(table, a.NewValues ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                        AddStyledCell(table, a.DateTimes.ToString("dd-MM-yyyy HH:mm:ss"), dataFont, rowColor, Element.ALIGN_CENTER);

                        rowCount++;
                    }

                    doc.Add(table);

                    // === Footer ===
                    var footerTable = new PdfPTable(1);
                    footerTable.WidthPercentage = 100;

                    var footerText = new Paragraph();
                    footerText.Add(new Chunk("Report generated by Audit Log System | ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128))));
                    footerText.Add(new Chunk("Page 1 of 1", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128))));

                    var footerCell = new PdfPCell(footerText);
                    footerCell.Border = Rectangle.TOP_BORDER;
                    footerCell.BorderColor = new BaseColor(200, 200, 200);
                    footerCell.PaddingTop = 10;
                    footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    footerTable.AddCell(footerCell);

                    doc.Add(footerTable);
                    doc.Close();
                }

                return memoryStream.ToArray();
            }
        }

        // Helper untuk cell dengan warna + styling
        private void AddStyledCell(PdfPTable table, string text, Font font, BaseColor backgroundColor, int align = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = backgroundColor,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                PaddingTop = 4,
                PaddingBottom = 4,
                BorderColor = new BaseColor(200, 200, 200),
                Border = Rectangle.BOX
            };
            table.AddCell(cell);
        }


    }
}
