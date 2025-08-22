using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class OtaFileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OtaFileController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/otafiles
        [HttpGet]
        public async Task<IActionResult> GetOtaFiles()
        {
            var result = await _context.OtaFiles
                .Select(o => new
                {
                    Id = o.OtaId,
                    OtaName = o.OtaDesc,
                    OtaFilename = o.OtaFilename,
                    RegisterDate = o.OtaCreateDate
                })
                .ToListAsync();

            return Ok(result);
        }
    }
}