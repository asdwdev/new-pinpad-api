using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class OtaFile
    {
        [Key]
        public int OtaId { get; set; }                  // ota_id

        public string OtaDesc { get; set; }             // ota_desc

        public Guid OtaKey { get; set; }                // ota_key

        public string OtaAttachment { get; set; }       // ota_attachment

        public string OtaFilename { get; set; }         // ota_filename

        public int OtaStatus { get; set; }              // ota_status

        public string OtaCreateBy { get; set; }         // ota_createby
        public DateTime OtaCreateDate { get; set; }     // ota_createdate

        public string? OtaUpdateBy { get; set; }         // ota_updateby
        public DateTime? OtaUpdateDate { get; set; }     // ota_updatedate

        // Relasi navigasi
        public ICollection<OtaFileAssign> Assignments { get; set; }
    }
}
