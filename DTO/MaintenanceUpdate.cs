namespace NewPinpadApi.DTOs
{
    public class MaintenanceUpdateDto
    {
        public string PpadStatus { get; set; }          // Status perbaikan
        public string? PpadStatusRepair { get; set; } 
        public string PpadUpdatedBy { get; set; }     // User yang update
    }
}