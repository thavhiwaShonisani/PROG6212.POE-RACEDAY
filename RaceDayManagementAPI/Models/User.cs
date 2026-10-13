using System.Text.Json.Serialization;

namespace RaceDayManagementAPI.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Participant";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore] public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
        [JsonIgnore] public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
