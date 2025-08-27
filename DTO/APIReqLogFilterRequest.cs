using System.ComponentModel.DataAnnotations;

namespace NewPinpadApi.DTOs
{
    public class APIReqLogFilterRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
        public string? Proses { get; set; }
        public string? ReqBy { get; set; }
        public string? StatusCode { get; set; }

        // Default paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
