using System;

namespace NewPinpadApi.DTOs
{
    public class DeviceTranslogExportDto
    {
        public int TranslogId { get; set; }
        public string TranslogSn { get; set; }
        public string TranslogBranch { get; set; }
        public string TranslogTrxType { get; set; }
        public string? TranslogCardnum { get; set; }
        public string? TranslogAcctnum { get; set; }
        public decimal? TranslogAmount { get; set; }
        public string? TranslogCreateby { get; set; }
        public DateTime TranslogCreatedate { get; set; }
        public string? TranslogRc { get; set; }
        public string? TranslogRrn { get; set; }
        
        // Related data for export
        public string? PinpadTid { get; set; }
        public string? PinpadStatus { get; set; }
        public string? BranchName { get; set; }
        public string? BranchArea { get; set; }
        public string? TransactionTypeDesc { get; set; }
    }
}
