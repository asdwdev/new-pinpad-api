using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Models;

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

        // GET: api/audit/pinpads
        [HttpGet("pinpads")]
        public async Task<ActionResult<IEnumerable<Audit>>> GetPinpadAudits()
        {
            var logs = await _context.Audits
                .Where(a => a.TableName == "Pinpads")
                .OrderByDescending(a => a.DateTimes)
                .ToListAsync();

            if (logs == null || !logs.Any())
                return NotFound(new { message = "Belum ada log Pinpad." });

            return Ok(logs);
        }
    }
}
