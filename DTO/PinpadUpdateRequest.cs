namespace NewPinpadApi.DTOs
{
    public class PinpadUpdateRequest
    {
        public string SerialNumber { get; set; }
        public string BranchCode { get; set; }
        public string Status { get; set; }
        public string? RepairStatus { get; set; }
        public string TerminalId { get; set; }
        public string? Flag { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
