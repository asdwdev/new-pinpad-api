namespace NewPinpadApi.Models
{
    public class Regional
    {
        public int Id { get; set; }
        public string Code { get; set; } // kode unik
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // Navigation property ke Branch
        public ICollection<Branch> Branches { get; set; }
    }
}