using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using NewPinpadApi.Services;
using NewPinpadApi.Middleware;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Koneksi ke database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cache & Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".NewPinpad.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.IsEssential = true;
});

// CORS untuk frontend (NewPinpadApp)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApp",
        policy => policy
            .WithOrigins("http://localhost:5221") // alamat frontend MVC
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    );
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// === Middleware pipeline ===
app.UseCors("AllowApp");
app.UseSession();
app.UseAPILogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// pakai HTTPS redirect
app.UseHttpsRedirection();

// ✅ serve static files (wwwroot otomatis aktif)
app.UseStaticFiles(); // otomatis expose wwwroot/*

// Pastikan folder wwwroot/uploads/otafiles sudah ada
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads", "otafiles");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Routing ke controllers (API)
app.MapControllers();

app.Run();
