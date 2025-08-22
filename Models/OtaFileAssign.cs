using System;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class OtaFileAssign
    {
        [Key]
        public int OtaassId { get; set; }          // otaass_id
        public Guid OtaassKey { get; set; }        // otaass_key (FK ke OtaFile.OtaKey)

        // Foreign key ke SysBranch (via Code)
        public string OtaassBranch { get; set; }   // otaass_branch

        // Relasi navigasi
        public SysBranch Branch { get; set; }
        public OtaFile OtaFile { get; set; }       // relasi ke OtaFile
    }
}
