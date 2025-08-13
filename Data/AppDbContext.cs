using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Models;

namespace NewPinpadApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tabel untuk Regional
        public DbSet<Regional> Regionals { get; set; }

        // Tabel untuk Branch
        public DbSet<Branch> Branches { get; set; }

        // Tabel untuk User
        public DbSet<User> Users { get; set; }

        // Tabel untuk Pinpad
        public DbSet<Pinpad> Pinpads { get; set; }
    }
}