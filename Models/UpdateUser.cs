namespace backend.Models
{
    public class UpdateUser
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }

        public int? Score { get; set; }
    }
}