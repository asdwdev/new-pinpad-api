using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewPinpadApi.Models
{
    public class DeviceTranslog
    {
        [Key]
        public int TranslogId { get; set; }
        
        [Required]
        public string TranslogSn { get; set; }             // Serial number - relates to Pinpad
        
        [Required]
        public string TranslogBranch { get; set; }         // Branch code - relates to SysBranch
        
        [Required]
        public string TranslogTrxType { get; set; }        // Transaction type - relates to SysResponseCode
        
        public string? TranslogCardnum { get; set; }       // Card number
        
        public string? TranslogAcctnum { get; set; }       // Account number
        
        public decimal? TranslogAmount { get; set; }       // Transaction amount
        
        public string? TranslogCreateby { get; set; }      // Created by user
        
        public DateTime TranslogCreatedate { get; set; }   // Created date
        
        public string? TranslogRc { get; set; }            // Response code
        
        public string? TranslogRrn { get; set; }           // Retrieval reference number

        // Navigation properties
        [ForeignKey("TranslogSn")]
        public Pinpad Pinpad { get; set; }
        
        [ForeignKey("TranslogBranch")]
        public SysBranch Branch { get; set; }
        
        [ForeignKey("TranslogTrxType")]
        public SysResponseCode TransactionType { get; set; }
    }
}
