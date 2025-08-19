using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Attributes;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // cari user berdasarkan username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            // validasi user, password, dan role
            if (user == null
                || HashPassword(request.Password) != user.Password
                || string.IsNullOrWhiteSpace(user.Role))
            {
                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }

            // set data ke session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                user = new
                {
                    user.Id,
                    user.Username,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }



        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // cek apakah user pernah Login (ada UserId di session)
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Logout failed or session not found"
                });
            }

            HttpContext.Session.Clear(); // hapus semua data session

            // kalau ada -> hapus semau data di session
            return Ok(new
            {
                success = true,
                message = "Logout successful"
            });
        }

        // [RequireSession]
        // [HttpGet("profile")]
        // public IActionResult GetProfile()
        // {
        //     // di sini pasti sudah login
        //     var username = HttpContext.Session.GetString("Username");
        //     return Ok(new { message = "Hello " + username });
        // }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            var username = HttpContext.Session.GetString("Username");
            var role = HttpContext.Session.GetString("Role");

            return Ok(new
            {
                success = true,
                username,
                role
            });
        }
    }
}