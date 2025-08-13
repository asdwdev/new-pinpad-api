using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;

namespace NewPinpadApi.Controller
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

            if (user == null)
            {
                return Unauthorized(new { sucess = false, message = "Invalid username or password" });
            }

            // hash password yang dikirim lalu cocokkan
            var hashedInputPassword = HashPassword(request.Password);
            if (user.Password != hashedInputPassword)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }

            // Set data ke session (server-side).
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username ?? "");
            HttpContext.Session.SetString("Role", user.Role ?? "User");

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

            // kalau ada -> hapus semau data di session
            return Ok(new
            {
                success = true,
                message = "Logout successful"
            });
        }
    }
}