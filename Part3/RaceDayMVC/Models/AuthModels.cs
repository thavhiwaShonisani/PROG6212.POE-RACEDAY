namespace RaceDayMVC.Models
{
    public class LoginViewModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
    public class RegisterViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Participant"; // "Organiser" or "Participant"
    }
    // Matches { message, token, user: { UserID, FullName, Email, Role } }
    public class AuthResponseVm
    {
        public string Message { get; set; } = "";
        public string Token { get; set; } = "";
        public AuthUserVm User { get; set; } = new();
    }
    public class AuthUserVm
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
    // Matches GET /api/auth/me
    public class MeResponseVm
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
