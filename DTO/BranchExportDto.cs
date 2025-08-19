namespace NewPinpadApi.DTOs
{
    public class BranchExportDto
    {
        public string KantorWilayah { get; set; } = "";
        public string KodeCabangInduk { get; set; } = "";
        public string CodeOutlet { get; set; } = "";
        public string NamaOutlet { get; set; } = "";
        public string Regional { get; set; } = "";
        public string KelasOutlet { get; set; } = "";
        public string IPLow { get; set; } = "";
        public string IPHigh { get; set; } = "";
        public int ID { get; set; }
        public DateTime? CreateDate { get; set; }
        public string CreateBy { get; set; } = "";
        public DateTime? UpdateDate { get; set; }
        public string UpdateBy { get; set; } = "";
    }
}