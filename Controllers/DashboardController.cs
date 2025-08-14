using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                // Group pinpad berdasarkan status dan hitung jumlah masing-masing
                var data = await _context.Pinpads
                    .GroupBy(p => p.PpadStatus)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Dashboard summary retrieved",
                    data
                });
            }
            catch (Exception ex)
            {
                // Kalau ada error
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve dashboard summary",
                    error = ex.Message
                });
            }
        }
    }
}