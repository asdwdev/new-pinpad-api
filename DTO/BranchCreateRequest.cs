namespace NewPinpadApi.DTOs
{
    public class BranchCreateRequest
    {
        public string Ctrlbr { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        // Relasi ke Area (pakai Code atau ID area)
        public string Area { get; set; }

        // Relasi ke BranchType (pakai Code atau ID)
        public string BranchType { get; set; }

        public string ppad_iplow { get; set; }
        public string ppad_iphigh { get; set; }
    }
}
