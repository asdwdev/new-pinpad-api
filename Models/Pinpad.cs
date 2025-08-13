using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class Pinpad
    {
        [Key]
        public int PpadId { get; set; }                // ID unik pinpad
        public string PpadSn { get; set; }             // Serial number
        public string PpadBranch { get; set; }         // Kode / nama branch saat ini
        public string PpadBranchLama { get; set; }     // Branch sebelumnya
        public string PpadStatus { get; set; }         // Status sekarang (misalnya aktif, rusak, dsb.)
        public string PpadStatusRepair { get; set; }   // Status perbaikan (jika ada)
        public string PpadStatusLama { get; set; }     // Status sebelumnya
        public string PpadTid { get; set; }            // TID (Terminal ID)
        public string PpadFlag { get; set; }           // Flag khusus (misal untuk penanda kondisi tertentu)
        public DateTime? PpadLastLogin { get; set; }   // Waktu terakhir login
        public DateTime? PpadLastActivity { get; set; }// Waktu terakhir aktivitas
        public string PpadCreateBy { get; set; }       // User yang membuat
        public DateTime PpadCreateDate { get; set; }   // Waktu dibuat
        public string PpadUpdateBy { get; set; }       // User yang mengupdate
        public DateTime? PpadUpdateDate { get; set; }  // Waktu terakhir update
    }
}