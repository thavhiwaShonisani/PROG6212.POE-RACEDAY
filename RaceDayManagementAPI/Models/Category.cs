using System.Text.Json.Serialization;

namespace RaceDayManagementAPI.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }

        public Event? Event { get; set; }

        [JsonIgnore] public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
