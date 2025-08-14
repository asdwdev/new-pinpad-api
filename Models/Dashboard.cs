namespace NewPinpadApi.Models
{
    public class Dashboard
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public int Total { get; set; }
        public int NotReady { get; set; }
        public int Ready { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int Maintenance { get; set; }
    }
}