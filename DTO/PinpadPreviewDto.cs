using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace NewPinpadApi.DTOs
{
    public class PinpadPreviewRow
    {
        public string Regional { get; set; } = string.Empty;
        public string CabangInduk { get; set; } = string.Empty;
        public string CabangOutlet { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;        // hasil mapping dari REMARK
        public string RemarkRaw { get; set; } = string.Empty;     // isi remark asli dari Excel
    }

    public class PinpadPreviewResponse
    {
        public bool ok { get; set; }
        public string message { get; set; } = string.Empty;
        public string[] columns { get; set; } = 
            { "Regional", "Cabang Induk", "Cabang Outlet", "Serial Number", "Status" };
        public int totalRows { get; set; }
        public List<PinpadPreviewRow> rows { get; set; } = new List<PinpadPreviewRow>();
    }

    public class PinpadUploadRequest
    {
        public IFormFile File { get; set; }
    }
}