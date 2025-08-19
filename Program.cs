using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Data;
using OfficeOpenXml;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// koneksi ke database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cache buat session
builder.Services.AddDistributedMemoryCache();

// Konfigurasi session cookie (HttpOnly, Secure)
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".NewPinpad.Session";
    options.Cookie.HttpOnly = true;
    // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // wajib HTTPS di prod
    options.Cookie.SameSite = SameSiteMode.Lax;              // aman untuk form login
    options.IdleTimeout = TimeSpan.FromMinutes(60);          // auto-expire kalau idle
    options.Cookie.IsEssential = true;                       // biar gak keblokir consent
});

try
{
    // EPPlus v5..v7
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
}
catch
{
    try
    {
        // Fallback for EPPlus v8+ where ExcelPackage.License may exist
        var epType = typeof(ExcelPackage);
        var licenseProp = epType.GetProperty("License", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (licenseProp != null)
        {
            var licenseType = licenseProp.PropertyType;
            // Try to find a static setter method on the license type (SetLicense / SetLicenseContext / Set)
            var setMethod = licenseType.GetMethod("SetLicense", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? licenseType.GetMethod("SetLicenseContext", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? licenseType.GetMethod("Set", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var enumType = epType.Assembly.GetType("OfficeOpenXml.LicenseContext") ?? epType.Assembly.GetType("OfficeOpenXml.License");
            if (setMethod != null && enumType != null)
            {
                var nonCommercial = Enum.Parse(enumType, "NonCommercial");
                setMethod.Invoke(null, new object[] { nonCommercial });
            }
        }
    }
    catch
    {
        // jika semua cara gagal, biarkan EPPlus melempar error saat digunakan — fallback CSV akan menolong
    }
}

// Tambahkan CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApp",
        policy => policy
            .WithOrigins("http://localhost:5221") // alamat NewPinpadApp lo
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials() // wajib kalau mau kirim session/cookie
    );
});

// tambahkan layanan controller
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// aktifkan session sebelum MapControllers
app.UseSession();

app.UseCors("AllowApp"); // HARUS sebelum app.MapControllers()


// aktifkan routing ke controllers
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
