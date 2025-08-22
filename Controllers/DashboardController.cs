using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;

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
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = new Dashboard
            {
                Total = await _context.Pinpads.CountAsync(),
                NotReady = await _context.Pinpads.CountAsync(p => p.PpadStatus == "NotReady"),
                Ready = await _context.Pinpads.CountAsync(p => p.PpadStatus == "Ready"),
                Active = await _context.Pinpads.CountAsync(p => p.PpadStatus == "Active"),
                Inactive = await _context.Pinpads.CountAsync(p => p.PpadStatus == "Inactive"),
                Maintenance = await _context.Pinpads.CountAsync(p => !string.IsNullOrEmpty(p.PpadStatusRepair))
            };

            return Ok(dashboard);
        }


    }
}