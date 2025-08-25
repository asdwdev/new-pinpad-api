using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Services;
using NewPinpadApi.Models;
using System.Data;
using ClosedXML.Excel;

namespace NewPinpadApi.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class PinpadPreviewController : ControllerBase
  {
    private readonly AppDbContext _context;
    private readonly IExcelService _excelService;
    private readonly ILogger<PinpadPreviewController> _logger;

    public PinpadPreviewController(
        AppDbContext context,
        IExcelService excelService,
        ILogger<PinpadPreviewController> logger)
    {
      _context = context;
      _excelService = excelService;
      _logger = logger;
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewPinpadExcel(IFormFile file)
    {
      try
      {
        // Validasi file
        if (file == null || file.Length == 0)
        {
          return Ok(new PinpadPreviewResponse
          {
            ok = false,
            message = "Upload file di field 'file'."
          });
        }

        // Validasi ekstensi file
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
          return Ok(new PinpadPreviewResponse
          {
            ok = false,
            message = "Gunakan file .xlsx atau .xls"
          });
        }

        DataTable dataTable;
        using (var stream = file.OpenReadStream())
        {
          dataTable = _excelService.ReadExcelToDataTable(stream);
        }

        if (dataTable.Rows.Count == 0)
        {
          return Ok(new PinpadPreviewResponse
          {
            ok = false,
            message = "File Excel kosong atau tidak memiliki data."
          });
        }

        // Build header mapping
        var headerMap = _excelService.BuildHeaderMap(
            dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
        );

        // Validasi header wajib
        if (!headerMap.ContainsKey("SERIAL_NUMBER") || !headerMap.ContainsKey("KODE_OUTLET"))
        {
          return Ok(new PinpadPreviewResponse
          {
            ok = false,
            message = "Header wajib: SERIAL_NUMBER & KODE_OUTLET."
          });
        }

        // Kumpulkan semua kode outlet untuk lookup batch
        var outletCodes = dataTable.AsEnumerable()
            .Select(row => _excelService.GetCellValue(row, headerMap, "KODE_OUTLET"))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Lookup branch info dengan join ke SysArea
        var branchLookup = await (from branch in _context.SysBranches
                                  join area in _context.SysAreas on branch.Area equals area.Code into areaGroup
                                  from area in areaGroup.DefaultIfEmpty()
                                  where outletCodes.Contains(branch.Code)
                                  select new
                                  {
                                    branch.Code,
                                    branch.Ctrlbr,
                                    AreaName = area != null ? area.Name : null
                                  })
                                .ToListAsync();

        var branchDict = branchLookup.ToDictionary(
            x => x.Code,
            x => x,
            StringComparer.OrdinalIgnoreCase
        );

        // Process rows untuk preview
        var previewRows = new List<PinpadPreviewRow>();

        foreach (DataRow row in dataTable.Rows)
        {
          var kodeOutlet = _excelService.GetCellValue(row, headerMap, "KODE_OUTLET");
          var serialNumber = _excelService.GetCellValue(row, headerMap, "SERIAL_NUMBER");
          var remark = _excelService.GetCellValue(row, headerMap, "REMARK");

          // Skip empty rows
          if (string.IsNullOrWhiteSpace(kodeOutlet) && string.IsNullOrWhiteSpace(serialNumber))
            continue;

          string regional = "-";
          string cabangInduk = "-";

          // Lookup branch info
          if (!string.IsNullOrWhiteSpace(kodeOutlet) &&
              branchDict.TryGetValue(kodeOutlet, out var branchInfo))
          {
            regional = string.IsNullOrWhiteSpace(branchInfo.AreaName) ? "-" : branchInfo.AreaName;
            cabangInduk = string.IsNullOrWhiteSpace(branchInfo.Ctrlbr) ? "-" : branchInfo.Ctrlbr;
          }

          var previewRow = new PinpadPreviewRow
          {
            Regional = regional,
            CabangInduk = cabangInduk,
            CabangOutlet = kodeOutlet ?? "-",
            SerialNumber = serialNumber ?? "-",
            Status = _excelService.MapRemarkToStatus(remark),
            RemarkRaw = remark ?? ""
          };

          previewRows.Add(previewRow);
        }

        var response = new PinpadPreviewResponse
        {
          ok = true,
          message = "Preview berhasil dibuat",
          totalRows = previewRows.Count,
          rows = previewRows
        };

        _logger.LogInformation("Excel preview generated successfully. Total rows: {RowCount}", previewRows.Count);

        return Ok(response);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing Excel preview");

        return Ok(new PinpadPreviewResponse
        {
          ok = false,
          message = "Gagal memproses file: " + ex.Message
        });
      }
    }

    [HttpGet("download-template")]
    public IActionResult DownloadTemplate()
    {
      try
      {
        // Create Excel template using ExcelService
        var excelBytes = _excelService.CreateExcelTemplate();

        var fileName = "Template_Multiple_Insert_Pinpad.xlsx";
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(excelBytes, contentType, fileName);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error generating template");
        return Ok(new { ok = false, message = "Gagal generate template: " + ex.Message });
      }
    }

    [HttpGet("sample-format")]
    public IActionResult GetSampleFormat()
    {
      var sampleData = new
      {
        requiredHeaders = new[] { "SERIAL_NUMBER", "KODE_OUTLET" },
        optionalHeaders = new[] { "REMARK" },
        sampleData = new[]
          {
                    new { SERIAL_NUMBER = "277115", KODE_OUTLET = "1302", REMARK = "SN Belum Terdaftar, Outlet Tidak Terdaftar" },
                    new { SERIAL_NUMBER = "297867", KODE_OUTLET = "1301", REMARK = "SN Sudah Terdaftar, Outlet Terdaftar" },
                    new { SERIAL_NUMBER = "277114", KODE_OUTLET = "0510", REMARK = "Data Sesuai" }
                },
        statusMapping = new Dictionary<string, string>
                {
                    { "SN Sudah Terdaftar", "SN sudah terdaftar" },
                    { "Outlet Tidak Terdaftar", "Outlet tidak terdaftar" },
                    { "Data Sesuai", "Data Sesuai" },
                    { "SN Belum Terdaftar + Outlet Terdaftar", "Data Sesuai" },
                    { "Default/Other", "Data tidak valid" }
                }
      };

      return Ok(new { success = true, data = sampleData });
    }

    [HttpPost("debug")]
    public IActionResult DebugData([FromBody] object data)
    {
      try
      {
        _logger.LogInformation("Received data: {Data}", System.Text.Json.JsonSerializer.Serialize(data));

        return Ok(new
        {
          ok = true,
          message = "Data received and logged",
          receivedData = data
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in debug endpoint");
        return Ok(new { ok = false, message = "Error: " + ex.Message });
      }
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddPinpad([FromBody] PinpadCreateDto dto)
    {
      if (dto == null || string.IsNullOrWhiteSpace(dto.SerialNumber) || string.IsNullOrWhiteSpace(dto.CabangOutlet))
      {
        return Ok(new { ok = false, message = "SerialNumber dan CabangOutlet wajib diisi." });
      }

      // cek outlet valid
      var branch = await (from b in _context.SysBranches
                          join a in _context.SysAreas on b.Area equals a.Code into areaGroup
                          from a in areaGroup.DefaultIfEmpty()
                          where b.Code == dto.CabangOutlet
                          select new
                          {
                            b.Code,
                            b.Ctrlbr,
                            Regional = a != null ? a.Name : "-"
                          }).FirstOrDefaultAsync();

      if (branch == null)
        return Ok(new { ok = false, message = "Kode outlet tidak ditemukan." });

      // cek SN unik
      bool snExists = await _context.Pinpads.AnyAsync(p => p.PpadSn == dto.SerialNumber);
      if (snExists)
        return Ok(new { ok = false, message = "SerialNumber sudah terdaftar." });

      // insert baru
      var pinpad = new Pinpad
      {
        PpadSn = dto.SerialNumber,
        PpadBranch = dto.CabangOutlet,
        PpadStatus = "NotReady", // Default status, bukan dari DTO
        PpadTid = "TEMP_" + dto.SerialNumber, // Add temporary TID
        PpadCreateBy = "system",
        PpadCreateDate = DateTime.Now
      };

      _context.Pinpads.Add(pinpad);
      await _context.SaveChangesAsync();

      // isi field tambahan buat response
      dto.Regional = branch.Regional;
      dto.CabangInduk = branch.Ctrlbr;

      return Ok(new { ok = true, message = "Pinpad berhasil ditambahkan.", data = dto });
    }

    [HttpPost("save")]
    public async Task<IActionResult> SavePinpad([FromBody] List<PinpadPreviewRow> rows)
    {
      if (rows == null || rows.Count == 0)
        return Ok(new { ok = false, message = "Tidak ada data untuk disimpan." });

      int inserted = 0, skipped = 0;
      var errors = new List<string>();

      foreach (var row in rows)
      {
        try
        {
          if (string.IsNullOrWhiteSpace(row.SerialNumber) || string.IsNullOrWhiteSpace(row.CabangOutlet))
          {
            skipped++;
            errors.Add($"Row SN:{row.SerialNumber} Outlet:{row.CabangOutlet} → kosong");
            continue;
          }

          var branch = await (from b in _context.SysBranches
                              join a in _context.SysAreas on b.Area equals a.Code into areaGroup
                              from a in areaGroup.DefaultIfEmpty()
                              where b.Code == row.CabangOutlet
                              select b).FirstOrDefaultAsync();

          if (branch == null)
          {
            skipped++;
            errors.Add($"Row SN:{row.SerialNumber} → Outlet {row.CabangOutlet} tidak ditemukan");
            continue;
          }

          bool snExists = await _context.Pinpads.AnyAsync(p => p.PpadSn == row.SerialNumber);
          if (snExists)
          {
            skipped++;
            errors.Add($"Row SN:{row.SerialNumber} → sudah ada di DB");
            continue;
          }

          var pinpad = new Pinpad
          {
            PpadSn = row.SerialNumber,
            PpadBranch = row.CabangOutlet,
            PpadStatus = "NotReady", // Default status, bukan dari REMARK
            PpadTid = "TEMP_" + row.SerialNumber, // Add temporary TID since it's required
            PpadCreateBy = "system",
            PpadCreateDate = DateTime.Now
          };

          _context.Pinpads.Add(pinpad);
          inserted++;
        }
        catch (Exception ex)
        {
          skipped++;
          errors.Add($"Row SN:{row.SerialNumber} → Error: {ex.Message}");
          _logger.LogError(ex, "Error processing row SN:{SerialNumber}", row.SerialNumber);
        }
      }

      try
      {
        await _context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error saving to database");

        // Get more detailed error information
        var innerException = ex.InnerException;
        var errorMessage = ex.Message;

        if (innerException != null)
        {
          errorMessage += $" Inner Exception: {innerException.Message}";
          _logger.LogError(innerException, "Inner exception details");
        }

        return Ok(new
        {
          ok = false,
          message = "Gagal menyimpan ke database: " + errorMessage,
          details = ex.ToString()
        });
      }

      return Ok(new
      {
        ok = true,
        message = $"Proses selesai. {inserted} data berhasil disimpan, {skipped} dilewati.",
        totalInserted = inserted,
        totalSkipped = skipped,
        details = errors
      });
    }



  }
}