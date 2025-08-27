namespace NewPinpadApi.DTOs
{
    public class OtaFileCreateRequest
    {
        public string OtaDesc { get; set; }
        public string OtaFilename { get; set; }
        public IFormFile OtaAttachment { get; set; }  // file fisik
        public int OtaStatus { get; set; }
    }

}