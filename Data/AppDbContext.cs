using Microsoft.EntityFrameworkCore;
using NewPinpadApi.Models;

namespace NewPinpadApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tabel untuk User
        public DbSet<User> Users { get; set; }

        // Tabel untuk Pinpad
        public DbSet<Pinpad> Pinpads { get; set; }

        // Tabel untuk SysAreas
        public DbSet<SysArea> SysAreas { get; set; }

        // Tabel untuk SysBranchTypes
        public DbSet<SysBranchType> SysBranchTypes { get; set; }

        // Tabel untuk SysBranches
        public DbSet<SysBranch> SysBranches { get; set; }


         // Tabel untuk Audit
        public DbSet<Audit> Audits { get; set; }

          // Tabel untuk SysResponseCode
        public DbSet<SysResponseCode> SysResponseCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SysBranch → SysArea
            modelBuilder.Entity<SysBranch>()
                .HasOne(b => b.SysArea)
                .WithMany(a => a.Branches)
                .HasForeignKey(b => b.Area)
                .HasPrincipalKey(a => a.Code);

            // SysBranch → SysBranchType
            modelBuilder.Entity<SysBranch>()
                .HasOne(b => b.SysBranchType)
                .WithMany(bt => bt.Branches)
                .HasForeignKey(b => b.Type)
                .HasPrincipalKey(bt => bt.Code);

            // Pinpad → SysBranch
            modelBuilder.Entity<Pinpad>()
                .HasOne(p => p.Branch)
                .WithMany(b => b.Pinpads)
                .HasForeignKey(p => p.PpadBranch)
                .HasPrincipalKey(b => b.Code);

            // Pinpad → SysResponseCode (Status Repair)
            modelBuilder.Entity<Pinpad>()
                .HasOne(p => p.StatusRepairCode)
                .WithMany(r => r.Pinpads)
                .HasForeignKey(p => p.PpadStatusRepair)
                .HasPrincipalKey(r => r.RescodeCode);

            base.OnModelCreating(modelBuilder);
        }


    }
}