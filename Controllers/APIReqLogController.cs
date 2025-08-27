using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Attributes;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;
using NewPinpadApi.Services;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequireSession] // kalau frontend butuh tanpa login, sementara bisa di-comment
    public class APIReqLogController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IExportService _exportService;

        public APIReqLogController(AppDbContext context, IExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        // ---------- Utility filter ----------
        private IQueryable<APIReqLog> ApplyFilters(IQueryable<APIReqLog> query, APIReqLogFilterRequest filter)
        {
            if (filter.StartDate.HasValue)
                query = query.Where(x => x.ReqDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(x => x.ReqDate.Date <= filter.EndDate.Value.Date);

            if (!string.IsNullOrEmpty(filter.Proses))
                query = query.Where(x => x.Proses != null && x.Proses.Contains(filter.Proses));

            if (!string.IsNullOrEmpty(filter.ReqBy))
                query = query.Where(x => x.ReqBy != null && x.ReqBy.Contains(filter.ReqBy));

            if (!string.IsNullOrEmpty(filter.StatusCode))
                query = query.Where(x => x.StatusCode != null && x.StatusCode.Contains(filter.StatusCode));

            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(x =>
                    (x.Proses != null && x.Proses.Contains(filter.Search)) ||
                    (x.ReqBy != null && x.ReqBy.Contains(filter.Search)) ||
                    (x.Request != null && x.Request.Contains(filter.Search)) ||
                    (x.Result != null && x.Result.Contains(filter.Search)) ||
                    (x.Remark != null && x.Remark.Contains(filter.Search))
                );
            }

            return query;
        }

        // ---------- GET with paging ----------
        [HttpGet]
        public async Task<IActionResult> GetAPIReqLogs([FromQuery] APIReqLogFilterRequest filter)
        {
            try
            {
                // fallback default kalau dikirim < 1
                if (filter.Page < 1) filter.Page = 1;
                if (filter.PageSize < 1) filter.PageSize = 20;

                var query = ApplyFilters(_context.APIReqLogs.AsQueryable(), filter);

                var totalCount = await query.CountAsync();
                var skip = (filter.Page - 1) * filter.PageSize;

                var logs = await query
                    .OrderByDescending(x => x.ReqDate)
                    .Skip(skip)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var result = new
                {
                    data = logs,
                    pagination = new
                    {
                        currentPage = filter.Page,
                        pageSize = filter.PageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                        hasNextPage = filter.Page * filter.PageSize < totalCount,
                        hasPreviousPage = filter.Page > 1
                    }
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                // tampilkan detail error biar gampang debug
                return StatusCode(500, new { success = false, message = ex.ToString() });
            }
        }

        // ---------- GET by Id ----------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAPIReqLog(int id)
        {
            var log = await _context.APIReqLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { success = false, message = "API Request Log not found" });

            return Ok(new { success = true, data = log });
        }

        // ---------- Export Excel ----------
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportToExcel([FromQuery] APIReqLogFilterRequest filter)
        {
            var data = await ApplyFilters(_context.APIReqLogs.AsQueryable(), filter)
                .OrderByDescending(x => x.ReqDate)
                .ToListAsync();

            var excelData = _exportService.ExportToExcel(data);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"APIReqLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        // ---------- Export PDF ----------
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportToPdf([FromQuery] APIReqLogFilterRequest filter)
        {
            var data = await ApplyFilters(_context.APIReqLogs.AsQueryable(), filter)
                .OrderByDescending(x => x.ReqDate)
                .ToListAsync();

            var pdfData = _exportService.ExportToPdf(data);
            return File(pdfData, "application/pdf",
                $"APIReqLog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        // ---------- Export Detailed PDF ----------
        [HttpGet("export/pdf/detailed")]
        public async Task<IActionResult> ExportToDetailedPdf([FromQuery] APIReqLogFilterRequest filter)
        {
            var data = await ApplyFilters(_context.APIReqLogs.AsQueryable(), filter)
                .OrderByDescending(x => x.ReqDate)
                .ToListAsync();

            var pdfData = _exportService.ExportToDetailedPdf(data);
            return File(pdfData, "application/pdf",
                $"APIReqLog_Detailed_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        // ---------- Export CSV ----------
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromQuery] APIReqLogFilterRequest filter)
        {
            var data = await ApplyFilters(_context.APIReqLogs.AsQueryable(), filter)
                .OrderByDescending(x => x.ReqDate)
                .ToListAsync();

            var csvData = _exportService.ExportToCsv(data);
            return File(csvData, "text/csv",
                $"APIReqLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ---------- Statistik ----------
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.APIReqLogs.AsQueryable();

            if (startDate.HasValue) query = query.Where(x => x.ReqDate.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(x => x.ReqDate.Date <= endDate.Value.Date);

            var total = await query.CountAsync();
            var success = await query.CountAsync(x => x.StatusCode == "200");
            var fail = total - success;
            var uniqueUsers = await query.Where(x => x.ReqBy != null).Select(x => x.ReqBy).Distinct().CountAsync();

            var topProses = await query
                .GroupBy(x => x.Proses)
                .Select(g => new { proses = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            var times = await query.Where(x => x.ResponseTime.HasValue).Select(x => x.ResponseTime.Value).ToListAsync();
            var avg = times.Any() ? times.Average() : 0;

            var stats = new
            {
                totalRequests = total,
                successfulRequests = success,
                failedRequests = fail,
                successRate = total > 0 ? Math.Round((double)success / total * 100, 2) : 0,
                uniqueUsers,
                topProcesses = topProses,
                responseTime = new
                {
                    average = Math.Round(avg, 2),
                    minimum = times.Any() ? times.Min() : 0,
                    maximum = times.Any() ? times.Max() : 0
                }
            };

            return Ok(new { success = true, data = stats });
        }

        // ---------- Hapus log lama ----------
        [HttpDelete("clear-old-logs")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysToKeep = 30)
        {
            var cutoff = DateTime.Now.AddDays(-daysToKeep);
            var oldLogs = await _context.APIReqLogs.Where(x => x.ReqDate < cutoff).ToListAsync();

            _context.APIReqLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Cleared {oldLogs.Count} logs older than {daysToKeep} days" });
        }
    }
}
