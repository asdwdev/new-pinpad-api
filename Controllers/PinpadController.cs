using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;

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

        // [HttpGet]
        // public async Task<IActionResult> GetAll(
        //      [FromQuery] string status = null,
        //      [FromQuery] string loc = null,
        //      [FromQuery] string q = null,
        //      [FromQuery] int page = 1,
        //      [FromQuery] int size = 10)
        // {
        //     // Ambil queryable dari database
        //     var query = _context.Pinpads.AsQueryable();

        //     // Filter by status jika ada
        //     if (!string.IsNullOrEmpty(status))
        //         query = query.Where(p => p.PpadStatus == status);

        //     // Filter by location (branch) jika ada
        //     if (!string.IsNullOrEmpty(loc))
        //         query = query.Where(p => p.PpadBranch == loc);

        //     // Search q di serial number atau TID
        //     if (!string.IsNullOrEmpty(q))
        //         query = query.Where(p => p.PpadSn.Contains(q) || p.PpadTid.Contains(q));

        //     // Total item sebelum paging
        //     var total = await query.CountAsync();

        //     // Paging
        //     var data = await query
        //         .OrderBy(p => p.PpadId)
        //         .Skip((page - 1) * size)
        //         .Take(size)
        //         .Select(p => new
        //         {
        //             ppadId = p.PpadId,
        //             ppadSn = p.PpadSn,
        //             ppadStatus = p.PpadStatus,
        //             ppadBranch = p.PpadBranch,
        //             ppadUpdateDate = p.PpadUpdateDate
        //         })
        //         .ToListAsync();

        //     return Ok(new
        //     {
        //         success = true,
        //         message = "Pinpad list retrieved",
        //         data,
        //         total
        //     });
        // }

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

        [HttpPut("{id}/maintenance")]
        public async Task<IActionResult> UpdateMaintenance(
            int id,
            [FromBody] MaintenanceUpdateDto request)
        {
            // Cari pinpad
            var pinpad = await _context.Pinpads.FindAsync(id);
            if (pinpad == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Maintenance update failed: Pinpad not found"
                });
            }

            try
            {
                // Simpan status lama
                pinpad.PpadStatusLama = pinpad.PpadStatus;

                // Update status
                pinpad.PpadStatus = request.PpadStatus;
                pinpad.PpadStatusRepair = request.PpadStatusRepair; // nullable
                pinpad.PpadUpdateBy = request.PpadUpdatedBy;
                pinpad.PpadUpdateDate = DateTime.Now;

                // Simpan log maintenance di DeviceLog
                var log = new DeviceLog
                {
                    DevlogBranch = pinpad.PpadBranch,
                    DevlogSn = pinpad.PpadSn,
                    DevlogStatus = request.PpadStatus,
                    DevlogTrxCode = "MAINTENANCE",
                    DevlogCreateBy = request.PpadUpdatedBy,
                    DevlogCreateDate = DateTime.Now
                };
                _context.DeviceLogs.Add(log);

                // Simpan ke database
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Pinpad maintenance updated successfully",
                    data = new
                    {
                        ppadId = pinpad.PpadId,
                        ppadSn = pinpad.PpadSn,
                        ppadStatusLama = pinpad.PpadStatusLama,
                        ppadStatus = pinpad.PpadStatus,
                        ppadStatusRepair = pinpad.PpadStatusRepair,
                        ppadUpdateDate = pinpad.PpadUpdateDate
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Maintenance update failed",
                    error = ex.Message
                });
            }
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

        // Endpoint: Simple Pinpad List
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




    }
}
