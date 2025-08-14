namespace NewPinpadApi.Models
{
    public class Audit
    {
        public int ID { get; set; }
        public string TableName { get; set; }
        public DateTime DateTimes { get; set; }
        public string KeyValues { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string Username { get; set; }
        public string ActionType { get; set; }
    }
}
