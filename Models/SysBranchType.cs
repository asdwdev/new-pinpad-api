namespace NewPinpadApi.Models
{
    public class SysBranchType
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime UpdateDate { get; set; }
        public string UpdateBy { get; set; }

        // Relasi: satu tipe bisa punya banyak branch
        public ICollection<SysBranch> Branches { get; set; }
    }
}
