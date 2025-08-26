using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.DTOs;
using NewPinpadApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace NewPinpadApi.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // --- GET ALL USERS ---
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            if (users == null || !users.Any())
                return NotFound(new { message = "No users found." });

            return Ok(users);
        }

        // --- GET USER BY ID ---
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found." });

            return Ok(user);
        }

        // --- CREATE USER ---
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Invalid user data." });

            // cek username sudah ada atau belum
            var existingUsername = await _context.Users
                .AnyAsync(u => u.Username == request.Username);
            if (existingUsername)
                return Conflict(new { message = "Username already exists." });

            // cek email sudah ada atau belum
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email == request.Email);
            if (existingEmail)
                return Conflict(new { message = "Email already exists." });

            var now = DateTime.Now;

            var user = new User
            {
                Username = request.Username,
                Password = HashPassword(request.Password),
                FullName = request.FullName,
                Email = request.Email,
                Type = request.Type,
                CreatedAt = now,
                CreatedBy = "SUP-ADM",
                AccessLevel = request.AccessLevel,
                Branch = request.Branch,
                IsLocked = request.IsLocked,
                Nip = request.Nip
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // audit
            var audit = new Audit
            {
                TableName = "Users",
                DateTimes = now,
                KeyValues = $"ID: {user.Id}",
                OldValues = "{}",
                NewValues = System.Text.Json.JsonSerializer.Serialize(user),
                Username = "SUP-ADM",
                ActionType = "Created"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // --- UPDATE USER ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found." });

            // cek username sudah dipakai user lain
            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username == request.Username && u.Id != id);
            if (usernameExists)
                return Conflict(new { message = "Username already exists." });

            // cek email sudah dipakai user lain
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != id);
            if (emailExists)
                return Conflict(new { message = "Email already exists." });

            var oldValues = System.Text.Json.JsonSerializer.Serialize(user);
            var now = DateTime.Now;

            // perbarui data yang boleh diubah
            user.Username = request.Username;
            user.FullName = request.FullName;
            user.Email = request.Email;
            user.Type = request.Type;
            user.AccessLevel = request.AccessLevel;
            user.Branch = request.Branch;
            user.IsLocked = request.IsLocked;
            user.Nip = request.Nip;

            // password hanya diperbarui jika diisi
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.Password = HashPassword(request.Password);

            user.UpdatedAt = now;
            user.UpdatedBy = "SUP-ADM";

            await _context.SaveChangesAsync();

            var newValues = System.Text.Json.JsonSerializer.Serialize(user);

            var audit = new Audit
            {
                TableName = "Users",
                DateTimes = now,
                KeyValues = $"ID: {user.Id}",
                OldValues = oldValues,
                NewValues = newValues,
                Username = "SUP-ADM",
                ActionType = "Updated"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        // --- DELETE USER ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found." });

            var oldValues = System.Text.Json.JsonSerializer.Serialize(user);
            var now = DateTime.Now;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            var audit = new Audit
            {
                TableName = "Users",
                DateTimes = now,
                KeyValues = $"ID: {user.Id}",
                OldValues = oldValues,
                NewValues = "{}",
                Username = "SUP-ADM",
                ActionType = "Deleted"
            };

            _context.Audits.Add(audit);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User with ID {id} deleted successfully." });
        }

        // --- HASH PASSWORD (SHA256) ---
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
