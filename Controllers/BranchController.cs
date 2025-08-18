using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]es")]
    public class BranchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/branches
        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.SysBranches
                .Include(b => b.SysArea) // join ke SysArea
                .OrderBy(b => b.ID)
                .Select(b => new
                {
                    AreaName = b.SysArea != null ? b.SysArea.Name : null, // ambil nama area
                    b.Ctrlbr,
                    b.Code,
                    b.Name,
                    BranchTypeName = b.SysBranchType != null ? b.SysBranchType.Name : null, // ambil nama branch type
                    b.ppad_iplow,
                    b.ppad_iphigh,
                })
                .ToListAsync();

            if (branches == null || !branches.Any())
            {
                return NotFound(new { message = "Data branch tidak ditemukan." });
            }

            return Ok(branches);
        }
    }
}