namespace NewPinpadApi.Models
{
    public class SysLevel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        // Relasi navigation property (One-to-Many)
        public ICollection<User> Users { get; set; }
    }
}