using System;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class Pinpad
    {
        [Key]
        public int PpadId { get; set; }                // ID unik pinpad
        public string PpadSn { get; set; }             // Serial number

        // Foreign key ke SysBranch.Code
        public string PpadBranch { get; set; }         // Kode branch saat ini
        public string? PpadBranchLama { get; set; }     // Branch sebelumnya

        public string PpadStatus { get; set; }         // Status sekarang (aktif, rusak, dsb.)
        public string? PpadStatusRepair { get; set; }  // Status perbaikan (jika ada)
        public string? PpadStatusLama { get; set; }     // Status sebelumnya
        public string PpadTid { get; set; }            // Terminal ID
        public string? PpadFlag { get; set; }           // Flag khusus

        public DateTime? PpadLastLogin { get; set; }
        public DateTime? PpadLastActivity { get; set; }

        public string? PpadCreateBy { get; set; }
        public DateTime PpadCreateDate { get; set; }
        public string? PpadUpdateBy { get; set; }
        public DateTime? PpadUpdateDate { get; set; }

        // Relasi navigasi
        public SysBranch Branch { get; set; }
        public SysResponseCode StatusRepairCode { get; set; }
    }
}
