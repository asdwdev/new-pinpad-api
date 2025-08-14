using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.Models
{
    public class SysResponseCode
    {
        [Key]
        public int RescodeId { get; set; }
        public string RescodeType { get; set; }
        public string RescodeCode { get; set; }
        public string RescodeDesc { get; set; }
        public string RescodeCreateBy { get; set; }
        public DateTime? RescodeCreateDate { get; set; }
        public string RescodeUpdateBy { get; set; }
        public DateTime? RescodeUpdateDate { get; set; }

        public ICollection<Pinpad> Pinpads { get; set; }
    }
}
