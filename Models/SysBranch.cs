using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class SysBranch
    {
        [Key]
        public int ID { get; set; }

        // Foreign key ke SysBranchType
        public string Type { get; set; }

        public string Code { get; set; }
        public string Ctrlbr { get; set; }
        public string Name { get; set; }

        // Foreign key ke SysArea
        public string Area { get; set; }

        public string ppad_iplow { get; set; }
        public string ppad_iphigh { get; set; }
        public int ppad_seq { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }

        // Relasi navigasi
        public SysArea SysArea { get; set; }
        public SysBranchType SysBranchType { get; set; }

        // Pinpad yang ada di cabang ini
        public ICollection<Pinpad> Pinpads { get; set; }
    }
}
