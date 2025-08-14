using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewPinpadApi.Models
{
    public class Pinpad
    {
        [Key]
        public int PpadId { get; set; }                // ID unik pinpad
        public string PpadSn { get; set; }             // Serial number
        
        [ForeignKey("Branch")]
        public int PpadBranch { get; set; }            // Foreign key ke Branch
        
        public int PpadBranchLama { get; set; }        // ID branch sebelumnya (integer)
        public string PpadStatus { get; set; }         // Status sekarang (Active, Inactive)
        public string PpadStatusRepair { get; set; }   // Status perbaikan (None, Repair)
        public string PpadStatusLama { get; set; }     // Status sebelumnya
        public string PpadTid { get; set; }            // TID (Terminal ID)
        public int PpadFlag { get; set; }              // Flag khusus (1, 0)
        public DateTime? PpadLastLogin { get; set; }   // Waktu terakhir login
        public DateTime? PpadLastActivity { get; set; }// Waktu terakhir aktivitas
        public string PpadCreateBy { get; set; }       // User yang membuat
        public DateTime PpadCreateDate { get; set; }   // Waktu dibuat
        public string PpadUpdateBy { get; set; }       // User yang mengupdate
        public DateTime? PpadUpdateDate { get; set; }  // Waktu terakhir update

        // Navigation properties
        public Branch Branch { get; set; }             // Relasi dengan Branch
    }
}
