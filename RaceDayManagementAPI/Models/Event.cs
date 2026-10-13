using System.Text.Json.Serialization;

namespace RaceDayManagementAPI.Models
{
    public class Event
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = "Run";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? Organiser { get; set; }

        [JsonIgnore] public ICollection<Category> Categories { get; set; } = new List<Category>();
        [JsonIgnore] public ICollection<RaceRoute> Routes { get; set; } = new List<RaceRoute>();
    }
}
