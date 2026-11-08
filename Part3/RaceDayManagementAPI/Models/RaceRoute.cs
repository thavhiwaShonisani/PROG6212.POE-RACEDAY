namespace RaceDayManagementAPI.Models
{
    public class RaceRoute
    {
        public int RouteID { get; set; }
        public int EventID { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string? MapFileUrl { get; set; }

        public Event? Event { get; set; }
    }
}
