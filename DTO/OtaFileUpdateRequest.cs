namespace NewPinpadApi.DTOs
{
    public class OtaFileUpdateRequest
    {
        public string OtaDesc { get; set; }
        public string OtaFilename { get; set; }
        public int OtaStatus { get; set; } = 1;
        public IFormFile? OtaAttachment { get; set; }
    }

}