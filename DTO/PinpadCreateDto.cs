using System;

namespace NewPinpadApi.DTOs
{
    public class PinpadCreateDto
    {
        public string SerialNumber { get; set; } = string.Empty;
        public string CabangOutlet { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // ini biar response balik lengkap
        public string Regional { get; set; } = string.Empty;
        public string CabangInduk { get; set; } = string.Empty;
    }
}
