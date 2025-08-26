using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.DTO
{
    public class SysMkeyCreateRequest
    {
        [Required]
        [MaxLength(100)]
        public string MkeyCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string MkeyNumber { get; set; }

        [MaxLength(255)]
        public string? MkeyDesc { get; set; }
    }
}


