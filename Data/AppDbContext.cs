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

        // Tabel untuk Dashboard
        public DbSet<Dashboard> Dashboards { get; set; }

        // Tabel untuk SysResponseCode
        public DbSet<SysResponseCode> SysResponseCodes { get; set; }

        // Tabel untuk OtaFile
        public DbSet<OtaFile> OtaFiles { get; set; }

        // Tabel untuk OtaFiles
        public DbSet<OtaFileAssign> OtaFileAssigns { get; set; }

        // Tabel untuk Device_Translog
        public DbSet<DeviceTranslog> DeviceTranslogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relasi SysBranch → SysArea
            modelBuilder.Entity<SysBranch>()
                .HasOne(b => b.SysArea)
                .WithMany(a => a.Branches)
                .HasForeignKey(b => b.Area)
                .HasPrincipalKey(a => a.Code);

            // Relasi SysBranch → SysBranchType
            modelBuilder.Entity<SysBranch>()
                .HasOne(b => b.SysBranchType)
                .WithMany(bt => bt.Branches)
                .HasForeignKey(b => b.Type)
                .HasPrincipalKey(bt => bt.Code);

            // Relasi Pinpad → SysBranch
            modelBuilder.Entity<Pinpad>()
                .HasOne(p => p.Branch)
                .WithMany(b => b.Pinpads)
                .HasForeignKey(p => p.PpadBranch)
                .HasPrincipalKey(b => b.Code);

            // Relasi Pinpad → SysResponseCode
            modelBuilder.Entity<Pinpad>()
                .HasOne(p => p.StatusRepairCode)
                .WithMany(r => r.Pinpads)
                .HasForeignKey(p => p.PpadStatusRepair)
                .HasPrincipalKey(r => r.RescodeCode);

            // Penting: disable OUTPUT clause untuk table Pinpads karena ada trigger
            modelBuilder.Entity<Pinpad>()
                .ToTable(tb => tb.UseSqlOutputClause(false));

            // Relasi OtaFile → OtaFileAssign (One-to-Many via OtaKey)
            modelBuilder.Entity<OtaFileAssign>()
                .HasOne(a => a.OtaFile)
                .WithMany(f => f.Assignments)
                .HasForeignKey(a => a.OtaassKey)
                .HasPrincipalKey(f => f.OtaKey);

            // OtaFileAssign → SysBranch
            modelBuilder.Entity<OtaFileAssign>()
                .HasOne(o => o.Branch)
                .WithMany(b => b.OtaFileAssigns)
                .HasForeignKey(o => o.OtaassBranch)
                .HasPrincipalKey(b => b.Code);

            // Relasi DeviceTranslog → Pinpad
            modelBuilder.Entity<DeviceTranslog>()
                .HasOne(dt => dt.Pinpad)
                .WithMany(p => p.DeviceTranslogs)
                .HasForeignKey(dt => dt.TranslogSn)
                .HasPrincipalKey(p => p.PpadSn)
                .OnDelete(DeleteBehavior.Restrict);

            // Relasi DeviceTranslog → SysBranch
            modelBuilder.Entity<DeviceTranslog>()
                .HasOne(dt => dt.Branch)
                .WithMany(b => b.DeviceTranslogs)
                .HasForeignKey(dt => dt.TranslogBranch)
                .HasPrincipalKey(b => b.Code)
                .OnDelete(DeleteBehavior.Restrict);

            // Relasi DeviceTranslog → SysResponseCode
            modelBuilder.Entity<DeviceTranslog>()
                .HasOne(dt => dt.TransactionType)
                .WithMany(r => r.DeviceTranslogs)
                .HasForeignKey(dt => dt.TranslogTrxType)
                .HasPrincipalKey(r => r.RescodeCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for TranslogAmount
            modelBuilder.Entity<DeviceTranslog>()
                .Property(dt => dt.TranslogAmount)
                .HasColumnType("decimal(18,2)");

            base.OnModelCreating(modelBuilder);
        }

    }
}