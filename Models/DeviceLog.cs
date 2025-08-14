using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class DeviceLog
    {
        [Key]
        public int DevlogId { get; set; }

        public string DevlogBranch { get; set; }

        public string DevlogSn { get; set; }

        public string DevlogStatus { get; set; }

        public string DevlogTrxCode { get; set; }

        public string DevlogCreateBy { get; set; }

        public DateTime DevlogCreateDate { get; set; }
    }
}