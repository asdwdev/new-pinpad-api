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

        // --- LOGIN ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // cari user berdasarkan username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            // validasi: user tidak ada atau password salah
            if (user == null || HashPassword(request.Password) != user.Password)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }

            // simpan data user ke session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("AccessLevel", user.AccessLevel.ToString());

            // response kalau berhasil login
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
                    user.AccessLevel
                }
            });
        }

        // --- HASH PASSWORD (SHA256) ---
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // ubah password ke bytes -> hash -> ubah ke hex string lowercase
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        // --- LOGOUT ---
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // cek apakah user sedang login (ada UserId di session)
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Logout failed or session not found"
                });
            }

            // hapus semua data di session
            HttpContext.Session.Clear();

            return Ok(new
            {
                success = true,
                message = "Logout successful"
            });
        }

        // --- GET CURRENT USER (ME) ---
        [HttpGet("me")]
        public IActionResult Me()
        {
            // cek apakah user sudah login
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Not logged in" });
            }

            // ambil data dari session
            var username = HttpContext.Session.GetString("Username") ?? "";
            var accessLevel = HttpContext.Session.GetString("AccessLevel") ?? "";

            // kirim info user saat ini
            return Ok(new
            {
                success = true,
                username,
                accessLevel
            });
        }
    }
}
