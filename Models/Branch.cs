namespace NewPinpadApi.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public int RegionalId { get; set; }
        public string Class { get; set; } // "Cabang" atau "Outlet"
        public string Code { get; set; }
        public string Name { get; set; }
        public string IpLow { get; set; }
        public string IpHigh { get; set; }
        public int? ParentBranchId { get; set; } // null kalau ini cabang induk
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public Regional Regional { get; set; }
        public Branch ParentBranch { get; set; }
        public ICollection<Branch> ChildBranches { get; set; }
        public ICollection<Pinpad> Pinpads { get; set; } // Relasi dengan Pinpad
    }
}