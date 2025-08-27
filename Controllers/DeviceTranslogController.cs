using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using ClosedXML.Excel;
using NewPinpadApi.Attributes;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTranslogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeviceTranslogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/DeviceTranslog
        [HttpGet]
        public async Task<IActionResult> GetDeviceTranslogs(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] string? regional,
    [FromQuery] string? outlet,
    [FromQuery] string? username,
    [FromQuery] string? serialNumber,
    [FromQuery] string? branch,
    [FromQuery] string? trxType,
    [FromQuery] string? cardNumber,
    [FromQuery] string? accountNumber,
    [FromQuery] string? q,
    [FromQuery] int page = 1,
    [FromQuery] int size = 100)
        {
            try
            {
                var query = _context.DeviceTranslogs
                    .Include(dt => dt.Pinpad)
                    .Include(dt => dt.Branch).ThenInclude(b => b.SysArea)
                    .Include(dt => dt.TransactionType)
                    .AsQueryable();

                // ✅ panggil ApplyFilters
                query = ApplyFilters(query, startDate, endDate, regional, outlet, username,
                                     serialNumber, branch, trxType, cardNumber, accountNumber, q);

                var totalData = await query.CountAsync();

                var result = await query
                    .OrderByDescending(dt => dt.TranslogCreatedate)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(dt => new
                    {
                        dt.TranslogId,
                        dt.TranslogSn,
                        dt.TranslogBranch,
                        dt.TranslogTrxType,
                        dt.TranslogCardnum,
                        dt.TranslogAcctnum,
                        dt.TranslogAmount,
                        dt.TranslogCreateby,
                        dt.TranslogCreatedate,
                        dt.TranslogRc,
                        dt.TranslogRrn,
                        pinpadTid = dt.Pinpad.PpadTid,
                        pinpadStatus = dt.Pinpad.PpadStatus,
                        branchName = dt.Branch.Name,
                        branchArea = dt.Branch.SysArea.Name,
                        transactionTypeDesc = dt.TransactionType.RescodeDesc
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Device transaction logs retrieved successfully",
                    data = result,
                    pagination = new
                    {
                        page,
                        size,
                        totalData,
                        totalPages = (int)Math.Ceiling((double)totalData / size)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve device transaction logs", error = ex.Message });
            }
        }



        // GET: api/DeviceTranslog/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviceTranslog(int id)
        {
            try
            {
                var deviceTranslog = await _context.DeviceTranslogs
                    .Include(dt => dt.Pinpad)
                    .Include(dt => dt.Branch)
                        .ThenInclude(b => b.SysArea)
                    .Include(dt => dt.TransactionType)
                    .FirstOrDefaultAsync(dt => dt.TranslogId == id);

                if (deviceTranslog == null)
                {
                    return NotFound(new { success = false, message = "Device transaction log not found" });
                }

                var result = new
                {
                    deviceTranslog.TranslogId,
                    deviceTranslog.TranslogSn,
                    deviceTranslog.TranslogBranch,
                    deviceTranslog.TranslogTrxType,
                    deviceTranslog.TranslogCardnum,
                    deviceTranslog.TranslogAcctnum,
                    deviceTranslog.TranslogAmount,
                    deviceTranslog.TranslogCreateby,
                    deviceTranslog.TranslogCreatedate,
                    deviceTranslog.TranslogRc,
                    deviceTranslog.TranslogRrn,
                    pinpad = new
                    {
                        deviceTranslog.Pinpad.PpadSn,
                        deviceTranslog.Pinpad.PpadTid,
                        deviceTranslog.Pinpad.PpadStatus
                    },
                    branch = new
                    {
                        deviceTranslog.Branch.Code,
                        deviceTranslog.Branch.Name,
                        area = deviceTranslog.Branch.SysArea.Name
                    },
                    transactionType = new
                    {
                        deviceTranslog.TransactionType.RescodeCode,
                        deviceTranslog.TransactionType.RescodeDesc
                    }
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to retrieve device transaction log", error = ex.Message });
            }
        }

        // POST: api/DeviceTranslog
        [HttpPost]
        public async Task<IActionResult> CreateDeviceTranslog([FromBody] DeviceTranslogCreateRequest request)
        {
            try
            {
                // Validate if Pinpad exists
                var pinpad = await _context.Pinpads.FirstOrDefaultAsync(p => p.PpadSn == request.TranslogSn);
                if (pinpad == null)
                {
                    return BadRequest(new { success = false, message = "Pinpad with the specified serial number not found" });
                }

                // Validate if Branch exists
                var branch = await _context.SysBranches.FirstOrDefaultAsync(b => b.Code == request.TranslogBranch);
                if (branch == null)
                {
                    return BadRequest(new { success = false, message = "Branch with the specified code not found" });
                }

                // Validate if Transaction Type exists
                var trxType = await _context.SysResponseCodes.FirstOrDefaultAsync(r => r.RescodeCode == request.TranslogTrxType);
                if (trxType == null)
                {
                    return BadRequest(new { success = false, message = "Transaction type with the specified code not found" });
                }

                var deviceTranslog = new DeviceTranslog
                {
                    TranslogSn = request.TranslogSn,
                    TranslogBranch = request.TranslogBranch,
                    TranslogTrxType = request.TranslogTrxType,
                    TranslogCardnum = request.TranslogCardnum,
                    TranslogAcctnum = request.TranslogAcctnum,
                    TranslogAmount = request.TranslogAmount,
                    TranslogCreateby = request.TranslogCreateby ?? "system",
                    TranslogCreatedate = DateTime.Now,
                    TranslogRc = request.TranslogRc,
                    TranslogRrn = request.TranslogRrn
                };

                _context.DeviceTranslogs.Add(deviceTranslog);
                await _context.SaveChangesAsync();

                // Create audit log
                var audit = new Audit
                {
                    TableName = "DeviceTranslog",
                    DateTimes = DateTime.Now,
                    KeyValues = deviceTranslog.TranslogId.ToString(),
                    OldValues = "{}",
                    NewValues = System.Text.Json.JsonSerializer.Serialize(deviceTranslog),
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Create"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDeviceTranslog), new { id = deviceTranslog.TranslogId }, new
                {
                    success = true,
                    message = "Device transaction log created successfully",
                    data = new { deviceTranslog.TranslogId }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to create device transaction log", error = ex.Message });
            }
        }

        // PUT: api/DeviceTranslog/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeviceTranslog(int id, [FromBody] DeviceTranslogUpdateRequest request)
        {
            try
            {
                var deviceTranslog = await _context.DeviceTranslogs.FindAsync(id);
                if (deviceTranslog == null)
                {
                    return NotFound(new { success = false, message = "Device transaction log not found" });
                }

                var oldValues = System.Text.Json.JsonSerializer.Serialize(deviceTranslog);

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.TranslogSn))
                {
                    var pinpad = await _context.Pinpads.FirstOrDefaultAsync(p => p.PpadSn == request.TranslogSn);
                    if (pinpad == null)
                    {
                        return BadRequest(new { success = false, message = "Pinpad with the specified serial number not found" });
                    }
                    deviceTranslog.TranslogSn = request.TranslogSn;
                }

                if (!string.IsNullOrEmpty(request.TranslogBranch))
                {
                    var branch = await _context.SysBranches.FirstOrDefaultAsync(b => b.Code == request.TranslogBranch);
                    if (branch == null)
                    {
                        return BadRequest(new { success = false, message = "Branch with the specified code not found" });
                    }
                    deviceTranslog.TranslogBranch = request.TranslogBranch;
                }

                if (!string.IsNullOrEmpty(request.TranslogTrxType))
                {
                    var trxType = await _context.SysResponseCodes.FirstOrDefaultAsync(r => r.RescodeCode == request.TranslogTrxType);
                    if (trxType == null)
                    {
                        return BadRequest(new { success = false, message = "Transaction type with the specified code not found" });
                    }
                    deviceTranslog.TranslogTrxType = request.TranslogTrxType;
                }

                if (request.TranslogCardnum != null)
                    deviceTranslog.TranslogCardnum = request.TranslogCardnum;

                if (request.TranslogAcctnum != null)
                    deviceTranslog.TranslogAcctnum = request.TranslogAcctnum;

                if (request.TranslogAmount.HasValue)
                    deviceTranslog.TranslogAmount = request.TranslogAmount;

                if (request.TranslogRc != null)
                    deviceTranslog.TranslogRc = request.TranslogRc;

                if (request.TranslogRrn != null)
                    deviceTranslog.TranslogRrn = request.TranslogRrn;

                await _context.SaveChangesAsync();

                // Create audit log
                var audit = new Audit
                {
                    TableName = "DeviceTranslog",
                    DateTimes = DateTime.Now,
                    KeyValues = deviceTranslog.TranslogId.ToString(),
                    OldValues = oldValues,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(deviceTranslog),
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Update"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Device transaction log updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to update device transaction log", error = ex.Message });
            }
        }

        // DELETE: api/DeviceTranslog/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeviceTranslog(int id)
        {
            try
            {
                var deviceTranslog = await _context.DeviceTranslogs.FindAsync(id);
                if (deviceTranslog == null)
                {
                    return NotFound(new { success = false, message = "Device transaction log not found" });
                }

                var oldValues = System.Text.Json.JsonSerializer.Serialize(deviceTranslog);

                _context.DeviceTranslogs.Remove(deviceTranslog);
                await _context.SaveChangesAsync();

                // Create audit log
                var audit = new Audit
                {
                    TableName = "DeviceTranslog",
                    DateTimes = DateTime.Now,
                    KeyValues = deviceTranslog.TranslogId.ToString(),
                    OldValues = oldValues,
                    NewValues = "{}",
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Delete"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Device transaction log deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to delete device transaction log", error = ex.Message });
            }
        }

        // GET: api/DeviceTranslog/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportDeviceTranslogs(
    string format = "csv",
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null,
    [FromQuery] string? regional = null,
    [FromQuery] string? outlet = null,
    [FromQuery] string? username = null,
    [FromQuery] string? serialNumber = null,
    [FromQuery] string? branch = null,
    [FromQuery] string? trxType = null,
    [FromQuery] string? cardNumber = null,
    [FromQuery] string? accountNumber = null,
    [FromQuery] string? q = null)
        {
            try
            {
                var query = _context.DeviceTranslogs
                    .Include(dt => dt.Pinpad)
                    .Include(dt => dt.Branch).ThenInclude(b => b.SysArea)
                    .Include(dt => dt.TransactionType)
                    .AsQueryable();

                // ✅ apply filter
                query = ApplyFilters(query, startDate, endDate, regional, outlet, username,
                                     serialNumber, branch, trxType, cardNumber, accountNumber, q);

                var translogs = await query
                    .OrderByDescending(dt => dt.TranslogCreatedate)
                    .Select(dt => new
                    {
                        dt.TranslogId,
                        dt.TranslogSn,
                        dt.TranslogBranch,
                        dt.TranslogTrxType,
                        dt.TranslogCardnum,
                        dt.TranslogAcctnum,
                        dt.TranslogAmount,
                        dt.TranslogCreateby,
                        dt.TranslogCreatedate,
                        dt.TranslogRc,
                        dt.TranslogRrn,
                        pinpadTid = dt.Pinpad.PpadTid,
                        pinpadStatus = dt.Pinpad.PpadStatus,
                        branchName = dt.Branch.Name,
                        branchArea = dt.Branch.SysArea.Name,
                        transactionTypeDesc = dt.TransactionType.RescodeDesc
                    })
                    .ToListAsync();

                if (!translogs.Any())
                    return NotFound(new { success = false, message = "No data found for export" });

                // ✅ Buat Audit Log Export
                var filters = new
                {
                    startDate,
                    endDate,
                    regional,
                    outlet,
                    username,
                    serialNumber,
                    branch,
                    trxType,
                    cardNumber,
                    accountNumber,
                    q
                };

                var audit = new Audit
                {
                    TableName = "DeviceTranslog",
                    DateTimes = DateTime.Now,
                    KeyValues = "Export",
                    OldValues = "{}",
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ExportFormat = format,
                        Filters = filters,
                        ResultCount = translogs.Count
                    }),
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Export"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                // ✅ Generate file sesuai format
                return format.ToLower() switch
                {
                    "csv" => File(GenerateDeviceTranslogCsv(translogs), "text/csv", "DeviceTranslogExport.csv"),
                    "xlsx" => File(GenerateDeviceTranslogExcel(translogs), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DeviceTranslogExport.xlsx"),
                    "pdf" => File(GenerateDeviceTranslogPdf(translogs), "application/pdf", "DeviceTranslogExport.pdf"),
                    _ => BadRequest(new { success = false, message = "Unsupported format" })
                };
                
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Export failed", error = ex.Message });
            }
        }



        // GET: api/DeviceTranslog/GetAvailableFilters
        [HttpGet("GetAvailableFilters")]
        public async Task<IActionResult> GetAvailableFilters()
        {
            try
            {
                var availableSerialNumbers = await _context.DeviceTranslogs
                    .Select(dt => dt.TranslogSn)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var availableBranches = await _context.DeviceTranslogs
                    .Select(dt => dt.TranslogBranch)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var availableTrxTypes = await _context.DeviceTranslogs
                    .Select(dt => dt.TranslogTrxType)
                    .Distinct()
                    .Take(10)
                    .ToListAsync();

                var availableAreas = await _context.SysAreas
                    .Select(a => a.Name)
                    .Distinct()
                    .ToListAsync();

                var result = new
                {
                    success = true,
                    message = "Available filter values retrieved",
                    data = new
                    {
                        availableSerialNumbers,
                        availableBranches,
                        availableTrxTypes,
                        availableAreas,
                        totalTranslogs = await _context.DeviceTranslogs.CountAsync()
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Failed to get available filters", error = ex.Message });
            }
        }

        // Helper methods for file generation
        private byte[] GenerateDeviceTranslogCsv(IEnumerable<dynamic> translogs)
        {
            var csv = new StringWriter();
            var csvHeader = "ID,Serial Number,Branch,Transaction Type,Card Number,Account Number,Amount,Create By,Created Date,Response Code,RRN,Pinpad TID,Pinpad Status,Branch Name,Branch Area,Transaction Description";
            csv.WriteLine(csvHeader);

            foreach (var tl in translogs)
            {
                var csvRow = string.Join(",",
                    tl.TranslogId,
                    tl.TranslogSn ?? "",
                    tl.TranslogBranch ?? "",
                    tl.TranslogTrxType ?? "",
                    tl.TranslogCardnum ?? "",
                    tl.TranslogAcctnum ?? "",
                    tl.TranslogAmount?.ToString() ?? "",
                    tl.TranslogCreateby ?? "",
                    tl.TranslogCreatedate.ToString("dd-MM-yyyy HH:mm:ss"),
                    tl.TranslogRc ?? "",
                    tl.TranslogRrn ?? "",
                    tl.pinpadTid ?? "",
                    tl.pinpadStatus ?? "",
                    tl.branchName ?? "",
                    tl.branchArea ?? "",
                    tl.transactionTypeDesc ?? ""
                );

                csv.WriteLine(csvRow);
            }

            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] GenerateDeviceTranslogExcel(IEnumerable<dynamic> translogs)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Device Transaction Logs");

            // Headers
            string[] headers = {
                "ID", "Serial Number", "Branch", "Transaction Type", "Card Number", "Account Number",
                "Amount", "Create By", "Created Date", "Response Code", "RRN", "Pinpad TID",
                "Pinpad Status", "Branch Name", "Branch Area", "Transaction Description"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;

            int r = 2;
            foreach (var tl in translogs)
            {
                ws.Cell(r, 1).Value = tl.TranslogId;
                ws.Cell(r, 2).Value = tl.TranslogSn ?? "";
                ws.Cell(r, 3).Value = tl.TranslogBranch ?? "";
                ws.Cell(r, 4).Value = tl.TranslogTrxType ?? "";
                ws.Cell(r, 5).Value = tl.TranslogCardnum ?? "";
                ws.Cell(r, 6).Value = tl.TranslogAcctnum ?? "";
                ws.Cell(r, 7).Value = tl.TranslogAmount?.ToString() ?? "";
                ws.Cell(r, 8).Value = tl.TranslogCreateby ?? "";

                if (tl.TranslogCreatedate is DateTime cd)
                {
                    ws.Cell(r, 9).Value = cd;
                    ws.Cell(r, 9).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
                }
                else ws.Cell(r, 9).Value = "";

                ws.Cell(r, 10).Value = tl.TranslogRc ?? "";
                ws.Cell(r, 11).Value = tl.TranslogRrn ?? "";
                ws.Cell(r, 12).Value = tl.pinpadTid ?? "";
                ws.Cell(r, 13).Value = tl.pinpadStatus ?? "";
                ws.Cell(r, 14).Value = tl.branchName ?? "";
                ws.Cell(r, 15).Value = tl.branchArea ?? "";
                ws.Cell(r, 16).Value = tl.transactionTypeDesc ?? "";

                r++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private byte[] GenerateDeviceTranslogPdf(IEnumerable<dynamic> translogs)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30))
                {
                    PdfWriter.GetInstance(doc, memoryStream);
                    doc.Open();

                    // Header
                    var headerTable = new PdfPTable(2) { WidthPercentage = 100 };
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    var companyCell = new PdfPCell(new Phrase(
                        "DEVICE TRANSACTION LOG SYSTEM",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64))
                    ))
                    { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT, PaddingBottom = 10 };
                    headerTable.AddCell(companyCell);

                    var exportInfo = new Paragraph();
                    exportInfo.Add(new Chunk("Generated: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                    exportInfo.Add(new Chunk("\nTotal Records: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(translogs.Count().ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                    var exportCell = new PdfPCell(exportInfo) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT };
                    headerTable.AddCell(exportCell);

                    doc.Add(headerTable);
                    doc.Add(new Paragraph(" "));

                    // Title
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(255, 255, 255));
                    var title = new Paragraph("DEVICE TRANSACTION LOG EXPORT", titleFont) { Alignment = Element.ALIGN_CENTER };

                    var titleCell = new PdfPCell(title)
                    {
                        BackgroundColor = new BaseColor(68, 114, 196),
                        Border = Rectangle.NO_BORDER,
                        PaddingTop = 8,
                        PaddingBottom = 8,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };

                    var titleTable = new PdfPTable(1) { WidthPercentage = 100 };
                    titleTable.AddCell(titleCell);
                    doc.Add(titleTable);
                    doc.Add(new Paragraph(" "));

                    // Table
                    var table = new PdfPTable(16) { WidthPercentage = 100, SpacingBefore = 10, SpacingAfter = 10 };
                    table.SetWidths(new float[] { 0.8f, 1.5f, 1.2f, 1.5f, 1.2f, 1.2f, 1.2f, 1.2f, 1.5f, 1.2f, 1.2f, 1.2f, 1.2f, 1.5f, 1.5f, 2f });

                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, new BaseColor(255, 255, 255));
                    var headerBackground = new BaseColor(68, 114, 196);
                    var headers = new[] { "ID", "Serial Number", "Branch", "Trx Type", "Card Num", "Acct Num", "Amount", "Create By", "Created Date", "RC", "RRN", "TID", "Status", "Branch Name", "Area", "Description" };

                    foreach (var h in headers)
                    {
                        var headerCell = new PdfPCell(new Phrase(h, headerFont))
                        {
                            BackgroundColor = headerBackground,
                            Border = Rectangle.BOTTOM_BORDER,
                            BorderColor = new BaseColor(255, 255, 255),
                            BorderWidthBottom = 2,
                            PaddingTop = 6,
                            PaddingBottom = 6,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };
                        table.AddCell(headerCell);
                    }

                    // Data rows
                    var rowCount = 0;
                    var lightGray = new BaseColor(245, 245, 245);
                    var white = new BaseColor(255, 255, 255);
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 7);
                    var dataFontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7);

                    foreach (var tl in translogs)
                    {
                        var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                        AddStyledCell(table, tl.TranslogId.ToString(), dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogSn ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogBranch ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogTrxType ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogCardnum ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogAcctnum ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogAmount?.ToString() ?? "", dataFont, rowColor, Element.ALIGN_RIGHT);
                        AddStyledCell(table, tl.TranslogCreateby ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogCreatedate.ToString("dd-MM-yyyy"), dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogRc ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.TranslogRrn ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.pinpadTid ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.pinpadStatus ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.branchName ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.branchArea ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, tl.transactionTypeDesc ?? "", dataFont, rowColor, Element.ALIGN_LEFT);

                        rowCount++;
                    }

                    doc.Add(table);
                    doc.Close();
                }

                return memoryStream.ToArray();
            }
        }

        // Helper method untuk menambahkan cell dengan styling yang konsisten
        private void AddStyledCell(PdfPTable table, string text, Font font, BaseColor backgroundColor, int alignment)
        {
            var cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = backgroundColor;
            cell.Border = Rectangle.BOTTOM_BORDER | Rectangle.TOP_BORDER | Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
            cell.BorderColor = new BaseColor(200, 200, 200);
            cell.BorderWidth = 0.5f;
            cell.PaddingTop = 4;
            cell.PaddingBottom = 4;
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            table.AddCell(cell);
        }

        private IQueryable<DeviceTranslog> ApplyFilters(
    IQueryable<DeviceTranslog> query,
    DateTime? startDate, DateTime? endDate,
    string? regional, string? outlet, string? username,
    string? serialNumber, string? branch, string? trxType,
    string? cardNumber, string? accountNumber, string? q)
        {
            if (startDate.HasValue)
                query = query.Where(dt => dt.TranslogCreatedate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(dt => dt.TranslogCreatedate <= endDate.Value);

            if (!string.IsNullOrEmpty(username))
                query = query.Where(dt => dt.TranslogCreateby != null && dt.TranslogCreateby.Contains(username));

            if (!string.IsNullOrEmpty(outlet))
                query = query.Where(dt => dt.TranslogBranch != null && dt.TranslogBranch.Contains(outlet));

            if (!string.IsNullOrEmpty(regional))
                query = query.Where(dt => dt.Branch.SysArea != null &&
                                          (dt.Branch.SysArea.Code.Contains(regional) ||
                                           dt.Branch.SysArea.Name.Contains(regional)));

            if (!string.IsNullOrEmpty(serialNumber))
                query = query.Where(dt => dt.TranslogSn.Contains(serialNumber));

            if (!string.IsNullOrEmpty(branch))
                query = query.Where(dt => dt.TranslogBranch.Contains(branch));

            if (!string.IsNullOrEmpty(trxType))
                query = query.Where(dt => dt.TranslogTrxType.Contains(trxType));

            if (!string.IsNullOrEmpty(cardNumber))
                query = query.Where(dt => dt.TranslogCardnum != null && dt.TranslogCardnum.Contains(cardNumber));

            if (!string.IsNullOrEmpty(accountNumber))
                query = query.Where(dt => dt.TranslogAcctnum != null && dt.TranslogAcctnum.Contains(accountNumber));

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(dt =>
                    dt.TranslogSn.Contains(q) ||
                    dt.TranslogBranch.Contains(q) ||
                    dt.TranslogTrxType.Contains(q) ||
                    (dt.TranslogCardnum != null && dt.TranslogCardnum.Contains(q)) ||
                    (dt.TranslogAcctnum != null && dt.TranslogAcctnum.Contains(q)) ||
                    (dt.TranslogRrn != null && dt.TranslogRrn.Contains(q)) ||
                    (dt.TranslogCreateby != null && dt.TranslogCreateby.Contains(q)) ||
                    (dt.Branch.SysArea != null && dt.Branch.SysArea.Name.Contains(q)));
            }

            return query;
        }

    }
}
