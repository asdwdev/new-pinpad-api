using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewPinpadApi.Models
{
    [Table("SysMkey")]
    public class SysMkey
    {
        [Key]
        [Column("mkey_id")]
        public int MkeyId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("mkey_code")]
        public string MkeyCode { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("mkey_number")]
        public string MkeyNumber { get; set; }

        [MaxLength(255)]
        [Column("mkey_desc")]
        public string? MkeyDesc { get; set; }

        [MaxLength(100)]
        [Column("mkey_createby")]
        public string? MkeyCreateBy { get; set; }

        [Column("mkey_createdate")]
        public DateTime MkeyCreateDate { get; set; }

        [MaxLength(100)]
        [Column("mkey_updateby")]
        public string? MkeyUpdateBy { get; set; }

        [Column("mkey_updatedate")]
        public DateTime? MkeyUpdateDate { get; set; }
    }
}


