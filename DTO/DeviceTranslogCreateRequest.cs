using System;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.DTOs
{
    public class DeviceTranslogCreateRequest
    {
        [Required]
        public string TranslogSn { get; set; }
        
        [Required]
        public string TranslogBranch { get; set; }
        
        [Required]
        public string TranslogTrxType { get; set; }
        
        public string? TranslogCardnum { get; set; }
        
        public string? TranslogAcctnum { get; set; }
        
        public decimal? TranslogAmount { get; set; }
        
        public string? TranslogCreateby { get; set; }
        
        public string? TranslogRc { get; set; }
        
        public string? TranslogRrn { get; set; }
    }
}
