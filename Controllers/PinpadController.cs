using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]

    public class PinpadController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PinpadController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Cari pinpad berdasarkan ID
            var data = await _context.Pinpads
                .Where(p => p.PpadId == id)
                .Select(p => new
                {
                    ppadId = p.PpadId,
                    ppadSn = p.PpadSn,
                    ppadStatus = p.PpadStatus,
                    ppadBranch = p.PpadBranch,
                    ppadBranchLama = p.PpadBranchLama,
                    ppadStatusRepair = p.PpadStatusRepair,
                    ppadStatusLama = p.PpadStatusLama,
                    ppadTid = p.PpadTid,
                    ppadFlag = p.PpadFlag,
                    ppadLastLogin = p.PpadLastLogin,
                    ppadLastActivity = p.PpadLastActivity,
                    ppadCreateBy = p.PpadCreateBy,
                    ppadCreateDate = p.PpadCreateDate,
                    ppadUpdateBy = p.PpadUpdateBy,
                    ppadUpdateDate = p.PpadUpdateDate
                })
                .FirstOrDefaultAsync();

            if (data == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pinpad not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Pinpad detail retrieved",
                data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetPinpadDetails(
            [FromQuery] string? status,
            [FromQuery] string? loc,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            loc = string.IsNullOrWhiteSpace(loc) ? null : loc.Trim();
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            var query = _context.Pinpads
                .Include(p => p.Branch)
                    .ThenInclude(b => b.SysArea)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.PpadStatus == status);

            if (!string.IsNullOrEmpty(loc))
                query = query.Where(p => p.Branch.Name.Contains(loc));

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(p =>
                    p.PpadSn.Contains(q) ||
                    p.PpadTid.Contains(q) ||
                    p.Branch.Code.Contains(q) ||
                    p.Branch.Name.Contains(q));
            }

            var totalData = await query.CountAsync();

            var result = await query
                .OrderByDescending(p => p.PpadCreateDate) // DESC default
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new
                {
                    Regional = p.Branch.SysArea.Name,
                    CabangInduk = p.Branch.Ctrlbr,
                    KodeOutlet = p.Branch.Code,
                    Location = p.Branch.Name,
                    RegisterDate = p.PpadCreateDate,
                    UpdateDate = p.PpadUpdateDate,
                    SerialNumber = p.PpadSn,
                    TID = p.PpadTid,
                    StatusPinpad = p.PpadStatus,
                    CreateBy = p.PpadCreateBy,
                    IpLow = p.Branch.ppad_iplow,
                    IpHigh = p.Branch.ppad_iphigh,
                    LastActivity = p.PpadLastActivity
                })
                .ToListAsync();

            return Ok(new
            {
                Page = page,
                Size = size,
                TotalData = totalData,
                TotalPages = (int)Math.Ceiling(totalData / (double)size),
                Data = result
            });
        }

        [HttpGet("inquiry")]
        public async Task<IActionResult> GetSimplePinpadList(
            [FromQuery] string? status,
            [FromQuery] string? branch,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int size = 10)
        {
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            branch = string.IsNullOrWhiteSpace(branch) ? null : branch.Trim();
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            var query = _context.Pinpads
                .Include(p => p.Branch)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.PpadStatus == status);

            if (!string.IsNullOrEmpty(branch))
                query = query.Where(p => p.Branch.Code == branch);

            if (!string.IsNullOrEmpty(q))
                query = query.Where(p =>
                    p.PpadSn.Contains(q) ||
                    p.Branch.Code.Contains(q));

            var totalData = await query.CountAsync();

            var result = await query
                .OrderByDescending(p => p.PpadCreateDate) // DESC default
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new
                {
                    Branch = p.Branch.Code,
                    SerialNumber = p.PpadSn,
                    RegisterDate = p.PpadCreateDate,
                    Status = p.PpadStatus
                })
                .ToListAsync();

            return Ok(new
            {
                Page = page,
                Size = size,
                TotalData = totalData,
                TotalPages = (int)Math.Ceiling(totalData / (double)size),
                Data = result
            });
        }
        
        [HttpPut("{id}/maintenance")]
        public async Task<IActionResult> UpdateMaintenance(int id, [FromBody] MaintenanceUpdateDto req)
        {
            if (string.IsNullOrWhiteSpace(req.StatusRepair))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status repair cannot be empty"
                });
            }

            // Check if status exists in SysResponseCode
            var statusExists = await _context.SysResponseCodes
                .AnyAsync(r => r.RescodeCode == req.StatusRepair && r.RescodeType == "StatusRepair");

            if (!statusExists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid status repair code"
                });
            }

            var pinpad = await _context.Pinpads
                .FirstOrDefaultAsync(p => p.PpadId == id);

            if (pinpad == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pinpad not found"
                });
            }

            var oldStatusRepair = pinpad.PpadStatusRepair;

            pinpad.PpadStatusRepair = req.StatusRepair;
            pinpad.PpadUpdateBy = User?.Identity?.Name ?? "system";
            pinpad.PpadUpdateDate = DateTime.Now;

            // Save to Audit table
            var audit = new Audit
            {
                TableName = "Pinpad",
                DateTimes = DateTime.Now,
                KeyValues = $"PpadId: {pinpad.PpadId}",
                OldValues = oldStatusRepair != null
                    ? $"{{\"PpadStatusRepair\":\"{oldStatusRepair}\"}}"
                    : "{}",
                NewValues = $"{{\"PpadStatusRepair\":\"{req.StatusRepair}\"}}",
                Username = pinpad.PpadUpdateBy,
                ActionType = "Modified"
            };

            _context.Audits.Add(audit);

            try
            {
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Status repair updated successfully",
                    data = new
                    {
                        ppadId = pinpad.PpadId,
                        ppadStatusRepair = pinpad.PpadStatusRepair,
                        ppadUpdateBy = pinpad.PpadUpdateBy,
                        ppadUpdateDate = pinpad.PpadUpdateDate
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating status repair",
                    error = ex.Message
                });
            }
        }


         // GET: api/Pinpad/GetBranches
        [HttpGet("GetBranches")]
    public async Task<IActionResult> GetBranches(string? status = null)
    {
        try
        {
            var query = _context.SysBranches.Include(b => b.SysArea).AsQueryable();
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Type == status);
            }

            var branches = await query.ToListAsync();
            
            if (!branches.Any())
                return NotFound(new { success = false, message = "Tidak ada data branch yang ditemukan." });

            return Ok(new { success = true, data = branches });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Gagal mengambil data branch.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/GetRegionals
    [HttpGet("GetRegionals")]
    public async Task<IActionResult> GetRegionals()
    {
        try
        {
            var regionals = await _context.SysAreas.Include(r => r.Branches).ToListAsync();
            
            if (!regionals.Any())
                return NotFound(new { success = false, message = "Tidak ada data regional yang ditemukan." });

            return Ok(new { success = true, data = regionals });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Gagal mengambil data regional.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/DebugPinpads - Debug endpoint to see all pinpad data
    [HttpGet("DebugPinpads")]
    public async Task<IActionResult> DebugPinpads()
    {
        try
        {
            var allPinpads = await _context.Pinpads
                .Include(p => p.Branch)
                .ThenInclude(b => b.SysArea)
                .ToListAsync();
            
            var result = new
            {
                success = true,
                totalCount = allPinpads.Count,
                data = allPinpads.Select(p => new
                {
                    p.PpadId,
                    p.PpadSn,
                    p.PpadBranch,
                    p.PpadStatus,
                    p.PpadTid,
                    branchName = p.Branch?.Name,
                    regionalName = p.Branch?.SysArea?.Name
                })
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Debug gagal.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/GetPinpads
    [HttpGet("GetPinpads")]
    public async Task<IActionResult> GetPinpads(string? status = null)
    {
        try
        {
            var query = _context.Pinpads.AsQueryable();
            
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.PpadStatus.ToLower() == status.ToLower());
            }

            var pinpads = await query.ToListAsync();
            
            if (!pinpads.Any())
            {
                var debugInfo = new
                {
                    filterApplied = status,
                    totalPinpadsInDb = await _context.Pinpads.CountAsync(),
                    hasFilters = !string.IsNullOrEmpty(status)
                };
                
                return NotFound(new { 
                    success = false, 
                    message = "Tidak ada data pinpad yang ditemukan.",
                    debug = debugInfo
                });
            }

            return Ok(new { success = true, data = pinpads });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Gagal mengambil data pinpad.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/pinpads/export - Export pinpad sesuai filter
    [HttpGet("pinpads/export")]
    public async Task<IActionResult> ExportPinpads(
        string format = "csv",
        string? status = null,
        string? branch = null,
        string? serialNumber = null)
    {
        try
        {
            // Simulasi proses export yang membutuhkan waktu
            await Task.Delay(2000); // Tunggu 2 detik untuk simulasi

            var query = from p in _context.Pinpads
                       select new
                       {
                           PpadId = p.PpadId,
                           PpadSn = p.PpadSn ?? "",
                           PpadBranch = p.PpadBranch,
                           PpadBranchLama = p.PpadBranchLama,
                           PpadStatus = p.PpadStatus ?? "",
                           PpadStatusRepair = p.PpadStatusRepair ?? "",
                           PpadStatusLama = p.PpadStatusLama ?? "",
                           PpadTid = p.PpadTid ?? "",
                           PpadFlag = p.PpadFlag,
                           PpadLastLogin = p.PpadLastLogin,
                           PpadLastActivity = p.PpadLastActivity,
                           PpadCreateBy = p.PpadCreateBy ?? "",
                           PpadCreateDate = p.PpadCreateDate,
                           PpadUpdateBy = p.PpadUpdateBy,
                           PpadUpdateDate = p.PpadUpdateDate
                       };

            // Debug: Log the initial query count
            var initialCount = await query.CountAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.PpadStatus.ToLower() == status.ToLower());
            }
            
            if (!string.IsNullOrEmpty(branch))
            {
                query = query.Where(p => p.PpadBranch == branch);
            }
            
            if (!string.IsNullOrEmpty(serialNumber))
            {
                query = query.Where(p => p.PpadSn.ToLower().Contains(serialNumber.ToLower()));
            }

            var pinpads = await query.ToListAsync();
            
            // Debug: Log filter details
            var debugInfo = new
            {
                filtersApplied = new { status, branch, serialNumber },
                initialCount,
                finalCount = pinpads.Count,
                hasFilters = !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(branch) || !string.IsNullOrEmpty(serialNumber)
            };
            
            if (!pinpads.Any())
            {
                return NotFound(new { 
                    success = false, 
                    message = "Tidak ada data pinpad yang ditemukan dengan filter yang diberikan.",
                    debug = debugInfo
                });
            }

            switch (format.ToLower())
            {
                case "csv":
                    var csvFile = GeneratePinpadCsvFromAnonymous(pinpads);
                    return File(csvFile, "text/csv", "PinpadExport.csv");

                case "xlsx":
                    var excelFile = GeneratePinpadExcelFromAnonymous(pinpads);
                    return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PinpadExport.xlsx");

                case "pdf":
                    var pdfFile = GeneratePinpadPdfFromAnonymous(pinpads);
                    return File(pdfFile, "application/pdf", "PinpadExport.pdf");

                default:
                    return BadRequest(new { success = false, message = "Format tidak didukung. Gunakan 'csv', 'xlsx', atau 'pdf'." });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Export gagal.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/GetAvailableFilters - Show available filter values
    [HttpGet("GetAvailableFilters")]
    public async Task<IActionResult> GetAvailableFilters()
    {
        try
        {
            var availableStatuses = await _context.Pinpads
                .Where(p => !string.IsNullOrEmpty(p.PpadStatus))
                .Select(p => p.PpadStatus)
                .Distinct()
                .ToListAsync();
                
            var availableBranches = await _context.Pinpads
                .Select(p => p.PpadBranch)
                .Distinct()
                .ToListAsync();
                
            var availableSerialNumbers = await _context.Pinpads
                .Where(p => !string.IsNullOrEmpty(p.PpadSn))
                .Select(p => p.PpadSn)
                .Take(10) // Limit to first 10 for display
                .ToListAsync();

            var result = new
            {
                success = true,
                message = "Available filter values retrieved",
                data = new
                {
                    availableStatuses,
                    availableBranches,
                    availableSerialNumbers,
                    totalPinpads = await _context.Pinpads.CountAsync()
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get available filters.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/CheckData - Simple endpoint to check data existence
    [HttpGet("CheckData")]
    public async Task<IActionResult> CheckData()
    {
        try
        {
            var areaCount = await _context.SysAreas.CountAsync();
            var branchCount = await _context.SysBranches.CountAsync();
            var pinpadCount = await _context.Pinpads.CountAsync();
            
            // Check if there are any pinpads with specific statuses
            var activePinpads = await _context.Pinpads.Where(p => p.PpadStatus == "Active").CountAsync();
            var inactivePinpads = await _context.Pinpads.Where(p => p.PpadStatus == "Inactive").CountAsync();
            
            var result = new
            {
                success = true,
                message = "Data check completed",
                data = new
                {
                    areas = areaCount,
                    branches = branchCount,
                    totalPinpads = pinpadCount,
                    activePinpads,
                    inactivePinpads,
                    hasData = areaCount > 0 || branchCount > 0 || pinpadCount > 0
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Data check failed.", error = ex.Message });
        }
    }

    // GET: api/Pinpad/DatabaseStatus
    [HttpGet("DatabaseStatus")]
    public async Task<IActionResult> DatabaseStatus()
    {
        try
        {
            var areaCount = await _context.SysAreas.CountAsync();
            var branchCount = await _context.SysBranches.CountAsync();
            var pinpadCount = await _context.Pinpads.CountAsync();
            var userCount = await _context.Users.CountAsync();

            return Ok(new { 
                success = true, 
                message = "Status database berhasil diambil.",
                data = new {
                    areas = areaCount,
                    branches = branchCount,
                    pinpads = pinpadCount,
                    users = userCount,
                    hasData = areaCount > 0 || branchCount > 0 || pinpadCount > 0 || userCount > 0
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Gagal mengambil status database.", error = ex.Message });
        }
    }

    // POST: api/Pinpad/SeedSampleData
    [HttpPost("SeedSampleData")]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            // Check if data already exists
            if (await _context.SysAreas.AnyAsync())
            {
                return BadRequest(new { success = false, message = "Data sudah ada di database." });
            }

            // Create sample area
            var area = new SysArea
            {
                Code = "REG001",
                Name = "Regional Jakarta",
                CreateDate = DateTime.Now,
                CreateBy = "admin"
            };
            _context.SysAreas.Add(area);
            await _context.SaveChangesAsync();

            // Create sample branches
            var branches = new List<SysBranch>
            {
                new SysBranch
                {
                    Area = area.Code,
                    Type = "active",
                    Code = "B001",
                    Name = "Cabang Jakarta Pusat",
                    ppad_iplow = "192.168.1.1",
                    ppad_iphigh = "192.168.1.10",
                    CreateDate = DateTime.Now,
                    CreateBy = "admin",
                    UpdateDate = DateTime.Now,
                    UpdateBy = "admin"
                },
                new SysBranch
                {
                    Area = area.Code,
                    Type = "active",
                    Code = "B002",
                    Name = "Cabang Jakarta Selatan",
                    ppad_iplow = "192.168.2.1",
                    ppad_iphigh = "192.168.2.10",
                    CreateDate = DateTime.Now,
                    CreateBy = "admin",
                    UpdateDate = DateTime.Now,
                    UpdateBy = "admin"
                }
            };
            _context.SysBranches.AddRange(branches);
            await _context.SaveChangesAsync();

            // Create sample pinpads
            var pinpads = new List<Pinpad>
            {
                new Pinpad
                {
                    PpadSn = "SN001",
                    PpadBranch = "B001",
                    PpadBranchLama = "",
                    PpadStatus = "Active",
                    PpadStatusRepair = "None",
                    PpadStatusLama = "Inactive",
                    PpadTid = "TID001",
                    PpadFlag = "1",
                    PpadCreateBy = "admin",
                    PpadCreateDate = DateTime.Now
                },
                new Pinpad
                {
                    PpadSn = "SN002",
                    PpadBranch = "B002",
                    PpadBranchLama = "",
                    PpadStatus = "Active",
                    PpadStatusRepair = "None",
                    PpadStatusLama = "Inactive",
                    PpadTid = "TID002",
                    PpadFlag = "1",
                    PpadCreateBy = "admin",
                    PpadCreateDate = DateTime.Now
                }
            };
            _context.Pinpads.AddRange(pinpads);
            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true, 
                message = "Sample data berhasil ditambahkan.",
                data = new { area, branches, pinpads }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Gagal menambahkan sample data.", error = ex.Message });
        }
    }

    // Helper methods untuk generate file
    private byte[] GeneratePinpadCsv(List<Pinpad> pinpads)
    {
        var csv = new StringWriter();
        var csvHeader = "Regional,Cabang Induk,Kode Outlet,Location,Register,Update Date,Serial Number,TID,Status Pinpad,Create By,IP Low,IP High,Last Activity\n";
        csv.WriteLine(csvHeader);

        foreach (var pinpad in pinpads)
        {
            // Get branch and regional info using helper method
            var branchInfo = GetBranchInfo(pinpad.PpadBranch);
            var branch = branchInfo.Branch;
            var parentBranch = branchInfo.ParentBranch;

            var csvRow = $"{branch?.SysArea?.Name ?? ""},{parentBranch?.Code ?? branch?.Code ?? ""},{branch?.Code ?? ""},{branch?.Name ?? ""},{pinpad.PpadCreateDate:dd-MM-yyyy HH:mm:ss},{pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? ""},{pinpad.PpadSn ?? ""},{pinpad.PpadTid ?? ""},{pinpad.PpadStatus ?? ""},{pinpad.PpadCreateBy ?? ""},{branch?.ppad_iplow ?? ""},{branch?.ppad_iphigh ?? ""},{pinpad.PpadLastActivity?.ToString("dd-MM-yyyy HH:mm:ss") ?? ""}";
            csv.WriteLine(csvRow);
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GeneratePinpadExcel(List<Pinpad> pinpads)
    {
        try
        {
            // Try EPPlus first
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Pinpad Data");
                
                // Add headers sesuai dengan urutan yang benar
                worksheet.Cells[1, 1].Value = "Regional";
                worksheet.Cells[1, 2].Value = "Cabang Induk";
                worksheet.Cells[1, 3].Value = "Kode Outlet";
                worksheet.Cells[1, 4].Value = "Location";
                worksheet.Cells[1, 5].Value = "Register";
                worksheet.Cells[1, 6].Value = "Update Date";
                worksheet.Cells[1, 7].Value = "Serial Number";
                worksheet.Cells[1, 8].Value = "TID";
                worksheet.Cells[1, 9].Value = "Status Pinpad";
                worksheet.Cells[1, 10].Value = "Create By";
                worksheet.Cells[1, 11].Value = "IP Low";
                worksheet.Cells[1, 12].Value = "IP High";
                worksheet.Cells[1, 13].Value = "Last Activity";

                // Add data sesuai dengan urutan header yang baru
                for (int i = 0; i < pinpads.Count; i++)
                {
                    var pinpad = pinpads[i];
                    var row = i + 2;
                    
                    // Get branch and regional info using helper method
                    var branchInfo = GetBranchInfo(pinpad.PpadBranch);
                    var branch = branchInfo.Branch;
                    var parentBranch = branchInfo.ParentBranch;
                    
                    worksheet.Cells[row, 1].Value = branch?.SysArea?.Name ?? "";
                    worksheet.Cells[row, 2].Value = parentBranch?.Code ?? branch?.Code ?? "";
                    worksheet.Cells[row, 3].Value = branch?.Code ?? "";
                    worksheet.Cells[row, 4].Value = branch?.Name ?? "";
                    worksheet.Cells[row, 5].Value = pinpad.PpadCreateDate.ToString("dd-MM-yyyy HH:mm:ss");
                    worksheet.Cells[row, 6].Value = pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "";
                    worksheet.Cells[row, 7].Value = pinpad.PpadSn ?? "";
                    worksheet.Cells[row, 8].Value = pinpad.PpadTid ?? "";
                    worksheet.Cells[row, 9].Value = pinpad.PpadStatus ?? "";
                    worksheet.Cells[row, 10].Value = pinpad.PpadCreateBy ?? "";
                    worksheet.Cells[row, 11].Value = branch?.ppad_iplow ?? "";
                    worksheet.Cells[row, 12].Value = branch?.ppad_iphigh ?? "";
                    worksheet.Cells[row, 13].Value = pinpad.PpadLastActivity?.ToString("dd-MM-yyyy HH:mm:ss") ?? "";
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                return package.GetAsByteArray();
            }
        }
        catch (Exception ex)
        {
            // Fallback: Generate CSV with tab separator
            return GeneratePinpadCsvFallback(pinpads.Select(p => new
            {
                PpadId = p.PpadId,
                PpadSn = p.PpadSn ?? "",
                PpadBranch = p.PpadBranch,
                PpadBranchLama = p.PpadBranchLama,
                PpadStatus = p.PpadStatus ?? "",
                PpadStatusRepair = p.PpadStatusRepair ?? "",
                PpadStatusLama = p.PpadStatusLama ?? "",
                PpadTid = p.PpadTid ?? "",
                PpadFlag = p.PpadFlag,
                PpadLastLogin = p.PpadLastLogin,
                PpadLastActivity = p.PpadLastActivity,
                PpadCreateBy = p.PpadCreateBy ?? "",
                PpadCreateDate = p.PpadCreateDate,
                PpadUpdateBy = p.PpadUpdateBy,
                PpadUpdateDate = p.PpadUpdateDate
            }));
        }
    }

    private byte[] GeneratePinpadPdf(List<Pinpad> pinpads)
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
                var companyCell = new PdfPCell(new Phrase("PINPAD MANAGEMENT SYSTEM", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64)))); // Dark gray color
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
                exportInfo.Add(new Chunk(pinpads.Count.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                var exportCell = new PdfPCell(exportInfo);
                exportCell.Border = Rectangle.NO_BORDER;
                exportCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                exportCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                headerTable.AddCell(exportCell);

                doc.Add(headerTable);
                doc.Add(new Paragraph(" ")); // Spacing

                // Title dengan styling yang lebih menarik
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(255, 255, 255)); // White color
                var title = new Paragraph("PINPAD DATA EXPORT", titleFont);
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
                var table = new PdfPTable(13);
                table.WidthPercentage = 100;
                table.SpacingBefore = 10;
                table.SpacingAfter = 10;

                // Set column widths untuk optimasi layout
                table.SetWidths(new float[] { 2f, 1.5f, 1.5f, 2.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f });

                // Header styling
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255)); // White color
                var headerBackground = new BaseColor(68, 114, 196);

                // Headers dengan styling yang konsisten
                var headers = new[] { "Regional", "Cabang Induk", "Kode Outlet", "Location", "Register", "Update Date", "Serial Number", "TID", "Status Pinpad", "Create By", "IP Low", "IP High", "Last Activity" };
                
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

                foreach (var pinpad in pinpads)
                {
                    // Get branch and regional info using helper method
                    var branchInfo = GetBranchInfo(pinpad.PpadBranch);
                    var branch = branchInfo.Branch;
                    var parentBranch = branchInfo.ParentBranch;

                    // Alternating row colors
                    var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                    // Data cells
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                    var dataFontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);

                    // Regional
                    AddStyledCell(table, branch?.SysArea?.Name ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                    
                    // Cabang Induk
                    AddStyledCell(table, parentBranch?.Code ?? branch?.Code ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // Kode Outlet
                    AddStyledCell(table, branch?.Code ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // Location
                    AddStyledCell(table, branch?.Name ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                    
                    // Register Date
                    AddStyledCell(table, pinpad.PpadCreateDate.ToString("dd-MM-yyyy"), dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Update Date
                    AddStyledCell(table, pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy") ?? "-", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Serial Number
                    AddStyledCell(table, pinpad.PpadSn ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // TID
                    AddStyledCell(table, pinpad.PpadTid ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Status dengan warna
                    var statusCell = new PdfPCell(new Phrase(pinpad.PpadStatus ?? "", dataFont));
                    statusCell.BackgroundColor = GetStatusColor(pinpad.PpadStatus);
                    statusCell.Border = Rectangle.BOTTOM_BORDER | Rectangle.TOP_BORDER | Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                    statusCell.BorderColor = new BaseColor(200, 200, 200); // Light gray color
                    statusCell.PaddingTop = 4;
                    statusCell.PaddingBottom = 4;
                    statusCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    statusCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    table.AddCell(statusCell);
                    
                    // Create By
                    AddStyledCell(table, pinpad.PpadCreateBy ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // IP Low
                    AddStyledCell(table, branch?.ppad_iplow ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // IP High
                    AddStyledCell(table, branch?.ppad_iphigh ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Last Activity
                    AddStyledCell(table, pinpad.PpadLastActivity?.ToString("dd-MM-yyyy") ?? "-", dataFont, rowColor, Element.ALIGN_CENTER);

                    rowCount++;
                }

                doc.Add(table);

                // Footer dengan informasi tambahan
                var footerTable = new PdfPTable(1);
                footerTable.WidthPercentage = 100;
                
                var footerText = new Paragraph();
                footerText.Add(new Chunk("Report generated by Pinpad Management System | ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128)))); // Gray color
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

    // Fallback method untuk Excel jika EPPlus gagal
    private byte[] GeneratePinpadCsvFallback(IEnumerable<dynamic> pinpads)
    {
        // Generate CSV content tapi dengan format yang lebih rapi
        var csv = new StringWriter();
        
        // Add headers sesuai dengan urutan yang benar
        var headers = new[] {
            "Regional", "Cabang Induk", "Kode Outlet", "Location", "Register", 
            "Update Date", "Serial Number", "TID", "Status Pinpad", "Create By", 
            "IP Low", "IP High", "Last Activity"
        };
        
        csv.WriteLine(string.Join("\t", headers));

        // Add data sesuai dengan urutan header yang baru
        foreach (var pinpad in pinpads)
        {
            // Get branch and regional info using helper method
            var branchInfo = GetBranchInfo(pinpad.PpadBranch);
            var branch = branchInfo.Branch;
            var parentBranch = branchInfo.ParentBranch;

            var row = new[] {
                branch?.SysArea?.Name ?? "",
                parentBranch?.Code ?? branch?.Code ?? "",
                branch?.Code ?? "",
                branch?.Name ?? "",
                pinpad.PpadCreateDate.ToString("dd-MM-yyyy HH:mm:ss"),
                pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "",
                pinpad.PpadSn ?? "",
                pinpad.PpadTid ?? "",
                pinpad.PpadStatus ?? "",
                pinpad.PpadCreateBy ?? "",
                branch?.ppad_iplow ?? "",
                branch?.ppad_iphigh ?? "",
                pinpad.PpadLastActivity?.ToString("dd-MM-yyyy HH:mm:ss") ?? ""
            };
            
            csv.WriteLine(string.Join("\t", row));
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    // Helper method untuk mendapatkan branch dan regional info
    private BranchInfo GetBranchInfo(string branchCode)
    {
        var branch = _context.SysBranches
            .Include(b => b.SysArea)
            .FirstOrDefault(b => b.Code == branchCode);
        
        // Note: SysBranch doesn't have ParentBranchId, so we'll return null for parentBranch
        // If you need parent branch functionality, you'll need to implement it differently
            
        return new BranchInfo { Branch = branch, ParentBranch = null };
    }

    // Helper class untuk branch info
    private class BranchInfo
    {
        public SysBranch? Branch { get; set; }
        public SysBranch? ParentBranch { get; set; }
    }

    // Helper methods for anonymous type export
    private byte[] GeneratePinpadCsvFromAnonymous(IEnumerable<dynamic> pinpads)
    {
        var csv = new StringWriter();
        var csvHeader = "Regional,Cabang Induk,Kode Outlet,Location,Register,Update Date,Serial Number,TID,Status Pinpad,Create By,IP Low,IP High,Last Activity\n";
        csv.WriteLine(csvHeader);

        foreach (var pinpad in pinpads)
        {
            // Get branch and regional info using helper method
            var branchInfo = GetBranchInfo(pinpad.PpadBranch);
            var branch = branchInfo.Branch;
            var parentBranch = branchInfo.ParentBranch;

            var csvRow = $"{branch?.SysArea?.Name ?? ""},{parentBranch?.Code ?? branch?.Code ?? ""},{branch?.Code ?? ""},{branch?.Name ?? ""},{pinpad.PpadCreateDate:dd-MM-yyyy HH:mm:ss},{pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? ""},{pinpad.PpadSn ?? ""},{pinpad.PpadTid ?? ""},{pinpad.PpadStatus ?? ""},{pinpad.PpadCreateBy ?? ""},{branch?.ppad_iplow ?? ""},{branch?.ppad_iphigh ?? ""},{pinpad.PpadLastActivity?.ToString("dd-MM-yyyy HH:mm:ss") ?? ""}";
            csv.WriteLine(csvRow);
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GeneratePinpadExcelFromAnonymous(IEnumerable<dynamic> pinpads)
    {
        try
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Pinpad Data");
                
                // Add headers sesuai dengan urutan yang benar
                worksheet.Cells[1, 1].Value = "Regional";
                worksheet.Cells[1, 2].Value = "Cabang Induk";
                worksheet.Cells[1, 3].Value = "Kode Outlet";
                worksheet.Cells[1, 4].Value = "Location";
                worksheet.Cells[1, 5].Value = "Register";
                worksheet.Cells[1, 6].Value = "Update Date";
                worksheet.Cells[1, 7].Value = "Serial Number";
                worksheet.Cells[1, 8].Value = "TID";
                worksheet.Cells[1, 9].Value = "Status Pinpad";
                worksheet.Cells[1, 10].Value = "Create By";
                worksheet.Cells[1, 11].Value = "IP Low";
                worksheet.Cells[1, 12].Value = "IP High";
                worksheet.Cells[1, 13].Value = "Last Activity";

                // Add data sesuai dengan urutan header yang baru
                for (int i = 0; i < pinpads.Count(); i++)
                {
                    var pinpad = pinpads.ElementAt(i);
                    var row = i + 2;
                    
                    // Get branch and regional info using helper method
                    var branchInfo = GetBranchInfo(pinpad.PpadBranch);
                    var branch = branchInfo.Branch;
                    var parentBranch = branchInfo.ParentBranch;
                    
                    worksheet.Cells[row, 1].Value = branch?.SysArea?.Name ?? "";
                    worksheet.Cells[row, 2].Value = parentBranch?.Code ?? branch?.Code ?? "";
                    worksheet.Cells[row, 3].Value = branch?.Code ?? "";
                    worksheet.Cells[row, 4].Value = branch?.Name ?? "";
                    worksheet.Cells[row, 5].Value = pinpad.PpadCreateDate.ToString("dd-MM-yyyy HH:mm:ss");
                    worksheet.Cells[row, 6].Value = pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "";
                    worksheet.Cells[row, 7].Value = pinpad.PpadSn ?? "";
                    worksheet.Cells[row, 8].Value = pinpad.PpadTid ?? "";
                    worksheet.Cells[row, 9].Value = pinpad.PpadStatus ?? "";
                    worksheet.Cells[row, 10].Value = pinpad.PpadCreateBy ?? "";
                    worksheet.Cells[row, 11].Value = branch?.ppad_iplow ?? "";
                    worksheet.Cells[row, 12].Value = branch?.ppad_iphigh ?? "";
                    worksheet.Cells[row, 13].Value = pinpad.PpadLastActivity?.ToString("dd-MM-yyyy HH:mm:ss") ?? "";
                }

                worksheet.Cells.AutoFitColumns();
                
                return package.GetAsByteArray();
            }
        }
        catch (Exception ex)
        {
            // Fallback to CSV if Excel fails
            return GeneratePinpadCsvFallback(pinpads);
        }
    }

    private byte[] GeneratePinpadPdfFromAnonymous(IEnumerable<dynamic> pinpads)
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
                var companyCell = new PdfPCell(new Phrase("PINPAD MANAGEMENT SYSTEM", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64)))); // Dark gray color
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
                exportInfo.Add(new Chunk(pinpads.Count().ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                var exportCell = new PdfPCell(exportInfo);
                exportCell.Border = Rectangle.NO_BORDER;
                exportCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                exportCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                headerTable.AddCell(exportCell);

                doc.Add(headerTable);
                doc.Add(new Paragraph(" ")); // Spacing

                // Title dengan styling yang lebih menarik
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(255, 255, 255)); // White color
                var title = new Paragraph("PINPAD DATA EXPORT", titleFont);
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
                var table = new PdfPTable(13);
                table.WidthPercentage = 100;
                table.SpacingBefore = 10;
                table.SpacingAfter = 10;

                // Set column widths untuk optimasi layout
                table.SetWidths(new float[] { 2f, 1.5f, 1.5f, 2.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f });

                // Header styling
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(255, 255, 255)); // White color
                var headerBackground = new BaseColor(68, 114, 196);

                // Headers dengan styling yang konsisten
                var headers = new[] { "Regional", "Cabang Induk", "Kode Outlet", "Location", "Register", "Update Date", "Serial Number", "TID", "Status Pinpad", "Create By", "IP Low", "IP High", "Last Activity" };
                
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

                foreach (var pinpad in pinpads)
                {
                    // Get branch and regional info using helper method
                    var branchInfo = GetBranchInfo(pinpad.PpadBranch);
                    var branch = branchInfo.Branch;
                    var parentBranch = branchInfo.ParentBranch;

                    // Alternating row colors
                    var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                    // Data cells
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                    var dataFontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);

                    // Regional
                    AddStyledCell(table, branch?.SysArea?.Name ?? "", dataFont, rowColor, Element.ALIGN_LEFT);
                    
                    // Cabang Induk
                    AddStyledCell(table, parentBranch?.Code ?? branch?.Code ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // Kode Outlet
                    AddStyledCell(table, branch?.Code ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // Location
                    AddStyledCell(table, branch?.Name ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Register Date
                    AddStyledCell(table, pinpad.PpadCreateDate.ToString("dd-MM-yyyy"), dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Update Date
                    AddStyledCell(table, pinpad.PpadUpdateDate?.ToString("dd-MM-yyyy") ?? "-", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Serial Number
                    AddStyledCell(table, pinpad.PpadSn ?? "", dataFontBold, rowColor, Element.ALIGN_CENTER);
                    
                    // TID
                    AddStyledCell(table, pinpad.PpadTid ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Status dengan warna
                    var statusCell = new PdfPCell(new Phrase(pinpad.PpadStatus ?? "", dataFont));
                    statusCell.BackgroundColor = GetStatusColor(pinpad.PpadStatus);
                    statusCell.Border = Rectangle.BOTTOM_BORDER | Rectangle.TOP_BORDER | Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
                    statusCell.BorderColor = new BaseColor(200, 200, 200); // Light gray color
                    statusCell.PaddingTop = 4;
                    statusCell.PaddingBottom = 4;
                    statusCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    statusCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    table.AddCell(statusCell);
                    
                    // Create By
                    AddStyledCell(table, pinpad.PpadCreateBy ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // IP Low
                    AddStyledCell(table, branch?.ppad_iplow ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // IP High
                    AddStyledCell(table, branch?.ppad_iphigh ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                    
                    // Last Activity
                    AddStyledCell(table, pinpad.PpadLastActivity?.ToString("dd-MM-yyyy") ?? "-", dataFont, rowColor, Element.ALIGN_CENTER);

                    rowCount++;
                }

                doc.Add(table);

                // Footer dengan informasi tambahan
                var footerTable = new PdfPTable(1);
                footerTable.WidthPercentage = 100;
                
                var footerText = new Paragraph();
                footerText.Add(new Chunk("Report generated by Pinpad Management System | ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128)))); // Gray color
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
    private void AddStyledCell(PdfPTable table, string text, Font font, BaseColor backgroundColor, int alignment)
    {
        var cell = new PdfPCell(new Phrase(text, font));
        cell.BackgroundColor = backgroundColor;
        cell.Border = Rectangle.BOTTOM_BORDER | Rectangle.TOP_BORDER | Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER;
        cell.BorderColor = new BaseColor(200, 200, 200); // Light gray color
        cell.BorderWidth = 0.5f;
        cell.PaddingTop = 4;
        cell.PaddingBottom = 4;
        cell.HorizontalAlignment = alignment;
        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
        table.AddCell(cell);
    }

    // Helper method untuk mendapatkan warna status
    private BaseColor GetStatusColor(string status)
    {
        if (string.IsNullOrEmpty(status)) return new BaseColor(200, 200, 200); // Light gray color
        
        switch (status.ToLower())
        {
            case "active":
                return new BaseColor(198, 239, 206); // Light green
            case "inactive":
                return new BaseColor(255, 199, 206); // Light red
            case "repair":
                return new BaseColor(255, 235, 156); // Light yellow
            case "maintenance":
                return new BaseColor(189, 215, 238); // Light blue
            default:
                return new BaseColor(200, 200, 200); // Light gray color
        }

    }




    }
}
