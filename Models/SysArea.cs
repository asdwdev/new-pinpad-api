using System;

namespace NewPinpadApi.Models
{
    public class SysArea
    {
        public int ID { get; set; }                // Primary Key
        public string Code { get; set; }           // Kode area
        public string Name { get; set; }           // Nama area
        public DateTime? CreateDate { get; set; }  // Tanggal dibuat
        public string? CreateBy { get; set; }       // Dibuat oleh
        public DateTime? UpdateDate { get; set; }  // Tanggal terakhir update
        public string? UpdateBy { get; set; }       // Diupdate oleh

        // Navigation property ke SysBranch

        // Relasi: satu area punya banyak branch
        public ICollection<SysBranch> Branches { get; set; }
    }
}
