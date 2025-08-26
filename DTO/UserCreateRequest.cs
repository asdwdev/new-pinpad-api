namespace NewPinpadApi.DTOs
{
    public class UserCreateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int Type { get; set; }
        public string AccessLevel { get; set; }
        public string? Branch { get; set; }
        public bool IsLocked { get; set; }
        public string Nip { get; set; }
    }

}