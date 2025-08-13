using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controller
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

        [HttpGet]
        public async Task<IActionResult> GetAll(
             [FromQuery] string status = null,
             [FromQuery] string loc = null,
             [FromQuery] string q = null,
             [FromQuery] int page = 1,
             [FromQuery] int size = 10)
        {
            // Ambil queryable dari database
            var query = _context.Pinpads.AsQueryable();

            // Filter by status jika ada
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.PpadStatus == status);

            // Filter by location (branch) jika ada
            if (!string.IsNullOrEmpty(loc))
                query = query.Where(p => p.PpadBranch == loc);

            // Search q di serial number atau TID
            if (!string.IsNullOrEmpty(q))
                query = query.Where(p => p.PpadSn.Contains(q) || p.PpadTid.Contains(q));

            // Total item sebelum paging
            var total = await query.CountAsync();

            // Paging
            var data = await query
                .OrderBy(p => p.PpadId)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(p => new
                {
                    ppadId = p.PpadId,
                    ppadSn = p.PpadSn,
                    ppadStatus = p.PpadStatus,
                    ppadBranch = p.PpadBranch,
                    ppadUpdateDate = p.PpadUpdateDate
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Pinpad list retrieved",
                data,
                total
            });
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

        [HttpPut("{id}/maintenance")]
        public async Task<IActionResult> UpdateMaintenance(
            int id, 
            [FromBody] MaintenanceUpdateDto request)
        {
            // Cari pinpad berdasarkan ID
            var data = await _context.Pinpads.FindAsync(id);
            if (data == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Maintenance update failed: Pinpad not found"
                });
            }

            try
            {
                // Update status 
                data.PpadStatusLama = data.PpadStatus;  // simpan status lama
                data.PpadStatus = request.Type;           // update status saat ini
                data.PpadUpdateBy = request.UpdatedBy;
                data.PpadUpdateDate = DateTime.Now;


                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Pinpad maintenance updated successfully",
                    data = new
                    {
                        ppadId = data.PpadId,
                        ppadSn = data.PpadSn,
                        ppadStatus = data.PpadStatus,
                        ppadStatusRepair = data.PpadStatusRepair,
                        ppadUpdateDate = data.PpadUpdateDate
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

        // DTO untuk request body
        public class MaintenanceUpdateDto
        {
            public string Type { get; set; }          // Status perbaikan
            public string UpdatedBy { get; set; }     // User yang update
        }

    }
}
