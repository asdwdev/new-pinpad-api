using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewPinpadApi.Models
{
    [Table("APIReqLog")]
    public class APIReqLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Proses { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Request { get; set; }

        [Column(TypeName = "text")]
        public string? Result { get; set; }

        [StringLength(10)]
        public string? StatusCode { get; set; }

        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(100)]
        public string? ReqBy { get; set; }

        [Required]
        public DateTime ReqDate { get; set; }

        [StringLength(20)]
        public string? Method { get; set; }

        [StringLength(500)]
        public string? Endpoint { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public int? ResponseTime { get; set; } // dalam milliseconds
    }
}
