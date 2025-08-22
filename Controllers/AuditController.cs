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
    }
}
