using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
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

        // --- GET ALL USERS WITH FILTER ---
        [HttpGet]
        public async Task<IActionResult> GetUsers(
     string? username,
     string? fullName,
     string? email,
     int? accessLevel,
     string? branch,
     bool? isLocked
 )
        {
            var query = _context.Users
                .Include(u => u.SysLevel)
                .AsQueryable();

            if (!string.IsNullOrEmpty(username))
                query = query.Where(u => u.Username.Contains(username));

            if (!string.IsNullOrEmpty(fullName))
                query = query.Where(u => u.FullName.Contains(fullName));

            if (!string.IsNullOrEmpty(email))
                query = query.Where(u => u.Email.Contains(email));

            if (accessLevel.HasValue)
                query = query.Where(u => u.SysLevel.Id == accessLevel.Value);

            if (!string.IsNullOrEmpty(branch))
                query = query.Where(u => u.Branch == branch);

            if (isLocked.HasValue)
                query = query.Where(u => u.IsLocked == isLocked.Value);

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Nip,
                    u.FullName,
                    u.Email,
                    AccessLevel = u.SysLevel.Name,
                    u.Branch,
                    u.IsLocked,
                    u.CreatedAt
                })
                .ToListAsync();

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

            var existingNIP = await _context.Users
                .AnyAsync(u => u.Nip == request.Nip);
            if (existingNIP)
                return Conflict(new { message = "NIP already exists." });

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


        // === EXPORT USERS ===
        [HttpGet("export")]
        public async Task<IActionResult> ExportUsers(
            string format = "xlsx",
            [FromQuery] string? username = null,
            [FromQuery] string? fullName = null,
            [FromQuery] string? email = null,
            [FromQuery] int? accessLevel = null,
            [FromQuery] string? branch = null,
            [FromQuery] bool? isLocked = null
        )
        {
            try
            {
                var query = _context.Users.Include(u => u.SysLevel).AsQueryable();

                // === Apply filters ===
                if (!string.IsNullOrEmpty(username))
                    query = query.Where(u => u.Username.Contains(username));

                if (!string.IsNullOrEmpty(fullName))
                    query = query.Where(u => u.FullName.Contains(fullName));

                if (!string.IsNullOrEmpty(email))
                    query = query.Where(u => u.Email.Contains(email));

                if (accessLevel.HasValue)
                    query = query.Where(u => u.SysLevel.Id == accessLevel.Value);

                if (!string.IsNullOrEmpty(branch))
                    query = query.Where(u => u.Branch == branch);

                if (isLocked.HasValue)
                    query = query.Where(u => u.IsLocked == isLocked.Value);

                var users = await query
                    .OrderBy(u => u.Username)
                    .ToListAsync();

                if (!users.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tidak ada data user yang ditemukan dengan filter yang diberikan.",
                        filtersApplied = new { username, fullName, email, accessLevel, branch, isLocked }
                    });
                }

                // === Simpan log Export ke Audit ===
                var audit = new Audit
                {
                    TableName = "Users",
                    DateTimes = DateTime.Now,
                    KeyValues = "Export",
                    OldValues = "{}",
                    NewValues = $"{{\"ExportFormat\":\"{format}\",\"ResultCount\":{users.Count}}}",
                    Username = User?.Identity?.Name ?? "system",
                    ActionType = "Export"
                };

                _context.Audits.Add(audit);
                await _context.SaveChangesAsync();

                // === Generate file sesuai format ===
                switch (format.ToLower())
                {
                    case "xlsx":
                        var excelFile = GenerateUserExcel(users);
                        return File(
                            excelFile,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"UserExport_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                        );

                    case "csv":
                        var csvFile = GenerateUserCsv(users);
                        return File(
                            csvFile,
                            "text/csv",
                            $"UserExport_{DateTime.Now:yyyyMMddHHmmss}.csv"
                        );

                    case "pdf":
                        var pdfFile = GenerateUserPdf(users);
                        return File(
                            pdfFile,
                            "application/pdf",
                            $"UserExport_{DateTime.Now:yyyyMMddHHmmss}.pdf"
                        );

                    default:
                        return BadRequest(new { success = false, message = "Format tidak didukung. Pilih 'xlsx', 'csv', atau 'pdf'." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Export gagal.", error = ex.Message });
            }
        }

        // === GENERATE EXCEL UNTUK USER ===
        private byte[] GenerateUserExcel(List<User> users)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Users");

            // Header sesuai field user
            string[] headers = {
        "ID", "Username", "NIP", "Full Name", "Email",
        "Access Level", "Branch", "Is Locked", "Created At"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;

            int r = 2;
            foreach (var u in users)
            {
                ws.Cell(r, 1).Value = u.Id;
                ws.Cell(r, 2).Value = u.Username ?? "";
                ws.Cell(r, 3).Value = u.Nip ?? "";
                ws.Cell(r, 4).Value = u.FullName ?? "";
                ws.Cell(r, 5).Value = u.Email ?? "";
                ws.Cell(r, 6).Value = u.SysLevel?.Name ?? "";
                ws.Cell(r, 7).Value = u.Branch ?? "";
                ws.Cell(r, 8).Value = u.IsLocked ? "Yes" : "No";
                ws.Cell(r, 9).Value = u.CreatedAt;
                ws.Cell(r, 9).Style.DateFormat.Format = "dd-MM-yyyy HH:mm:ss";
                r++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // === GENERATE CSV UNTUK USER ===
        private byte[] GenerateUserCsv(List<User> users)
        {
            using var sw = new StringWriter();

            // Header
            var csvHeader = "ID,Username,NIP,Full Name,Email,Access Level,Branch,Is Locked,Created At";
            sw.WriteLine(csvHeader);

            foreach (var u in users)
            {
                var csvRow = string.Join(",",
                    u.Id,
                    EscapeCsv(u.Username),
                    EscapeCsv(u.Nip),
                    EscapeCsv(u.FullName),
                    EscapeCsv(u.Email),
                    EscapeCsv(u.SysLevel?.Name),
                    EscapeCsv(u.Branch),
                    u.IsLocked ? "Yes" : "No",
                    u.CreatedAt.ToString("dd-MM-yyyy HH:mm:ss")
                );

                sw.WriteLine(csvRow);
            }

            return Encoding.UTF8.GetBytes(sw.ToString());
        }

        // === HELPER ESCAPE CSV ===
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        // === GENERATE PDF UNTUK USER ===
        private byte[] GenerateUserPdf(List<User> users)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var doc = new Document(PageSize.A4.Rotate(), 20, 20, 30, 30)) // Landscape
                {
                    PdfWriter.GetInstance(doc, memoryStream);
                    doc.Open();

                    // === Header ===
                    var headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });

                    // Kiri (Judul Sistem)
                    var companyCell = new PdfPCell(new Phrase(
                        "USER MANAGEMENT SYSTEM",
                        FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(64, 64, 64))
                    ));
                    companyCell.Border = Rectangle.NO_BORDER;
                    companyCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    companyCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    companyCell.PaddingBottom = 10;
                    headerTable.AddCell(companyCell);

                    // Kanan (info export)
                    var exportInfo = new Paragraph();
                    exportInfo.Add(new Chunk("Generated: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));
                    exportInfo.Add(new Chunk("\nTotal Users: ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
                    exportInfo.Add(new Chunk(users.Count.ToString(), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)));

                    var exportCell = new PdfPCell(exportInfo);
                    exportCell.Border = Rectangle.NO_BORDER;
                    exportCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    exportCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    headerTable.AddCell(exportCell);

                    doc.Add(headerTable);
                    doc.Add(new Paragraph(" ")); // spacing

                    // === Title Section ===
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
                    var title = new Paragraph("USER EXPORT", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };

                    var titleCell = new PdfPCell(title);
                    titleCell.BackgroundColor = new BaseColor(68, 114, 196);
                    titleCell.Border = Rectangle.NO_BORDER;
                    titleCell.PaddingTop = 8;
                    titleCell.PaddingBottom = 8;
                    titleCell.HorizontalAlignment = Element.ALIGN_CENTER;

                    var titleTable = new PdfPTable(1);
                    titleTable.WidthPercentage = 100;
                    titleTable.AddCell(titleCell);
                    doc.Add(titleTable);
                    doc.Add(new Paragraph(" "));

                    // === Table ===
                    var table = new PdfPTable(9);
                    table.WidthPercentage = 100;
                    table.SpacingBefore = 10;
                    table.SpacingAfter = 10;

                    table.SetWidths(new float[] { 1f, 1.5f, 1.5f, 2.2f, 2.5f, 1.5f, 1.5f, 1f, 2f });

                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);
                    var headerBg = new BaseColor(68, 114, 196);

                    string[] headers = { "ID", "Username", "NIP", "Full Name", "Email", "Access Level", "Branch", "Is Locked", "Created At" };

                    foreach (var h in headers)
                    {
                        var cell = new PdfPCell(new Phrase(h, headerFont));
                        cell.BackgroundColor = headerBg;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.PaddingTop = 6;
                        cell.PaddingBottom = 6;
                        table.AddCell(cell);
                    }

                    // === Data Rows ===
                    var rowCount = 0;
                    var lightGray = new BaseColor(245, 245, 245);
                    var white = BaseColor.WHITE;
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                    foreach (var u in users)
                    {
                        var rowColor = (rowCount % 2 == 0) ? white : lightGray;

                        AddStyledCell(table, u.Id.ToString(), dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, u.Username ?? "", dataFont, rowColor);
                        AddStyledCell(table, u.Nip ?? "", dataFont, rowColor);
                        AddStyledCell(table, u.FullName ?? "", dataFont, rowColor);
                        AddStyledCell(table, u.Email ?? "", dataFont, rowColor);
                        AddStyledCell(table, u.SysLevel?.Name ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, u.Branch ?? "", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, u.IsLocked ? "Yes" : "No", dataFont, rowColor, Element.ALIGN_CENTER);
                        AddStyledCell(table, u.CreatedAt.ToString("dd-MM-yyyy HH:mm:ss"), dataFont, rowColor, Element.ALIGN_CENTER);

                        rowCount++;
                    }

                    doc.Add(table);

                    // === Footer ===
                    var footerTable = new PdfPTable(1);
                    footerTable.WidthPercentage = 100;

                    var footerText = new Paragraph();
                    footerText.Add(new Chunk("Report generated by User Management System | ", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128))));
                    footerText.Add(new Chunk("Page 1 of 1", FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(128, 128, 128))));

                    var footerCell = new PdfPCell(footerText);
                    footerCell.Border = Rectangle.TOP_BORDER;
                    footerCell.BorderColor = new BaseColor(200, 200, 200);
                    footerCell.PaddingTop = 10;
                    footerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    footerTable.AddCell(footerCell);

                    doc.Add(footerTable);
                    doc.Close();
                }

                return memoryStream.ToArray();
            }
        }

        private void AddStyledCell(PdfPTable table, string text, Font font, BaseColor backgroundColor, int align = Element.ALIGN_LEFT)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = backgroundColor,
                HorizontalAlignment = align,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                PaddingTop = 4,
                PaddingBottom = 4,
                BorderColor = new BaseColor(200, 200, 200),
                Border = Rectangle.BOX
            };
            table.AddCell(cell);
        }
    }
}
