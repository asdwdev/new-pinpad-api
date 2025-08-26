using System;
using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.DTOs
{
    public class DeviceTranslogUpdateRequest
    {
        public string? TranslogSn { get; set; }
        
        public string? TranslogBranch { get; set; }
        
        public string? TranslogTrxType { get; set; }
        
        public string? TranslogCardnum { get; set; }
        
        public string? TranslogAcctnum { get; set; }
        
        public decimal? TranslogAmount { get; set; }
        
        public string? TranslogRc { get; set; }
        
        public string? TranslogRrn { get; set; }
    }
}
