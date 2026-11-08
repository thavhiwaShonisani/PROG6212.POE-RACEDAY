namespace RaceDayMVC.Models
{
    // Matches Event JSON (Categories & Routes are [JsonIgnore] on API side)
    public class EventVm
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string EventName { get; set; } = "";
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = "";         // "Run" / "Walk" / "Cycle"
        public string? BannerImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserVm? Organiser { get; set; }
    }
    // Matches Category JSON
    public class CategoryVm
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public decimal EntryFee { get; set; }
    }
    // Matches User JSON (embedded in events and enrolments)
    public class UserVm
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
    // Matches Enrolment JSON
    public class EnrolmentVm
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = "";
        
        public UserVm? Participant { get; set; }
        public CategoryVm? Category { get; set; }
    }
    // Matches Result JSON (with nested Enrolment for filters)
    public class ResultVm
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = "";            // "Finished" / "DNF" / "DQ"
        public EnrolmentVm? Enrolment { get; set; }
    }
    // For create event form
    public class CreateEventVm
    {
        public string EventName { get; set; } = "";
        public string? Description { get; set; }
        public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1);
        public string Location { get; set; } = "";
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = "Run";      // Run/Walk/Cycle (exact case)
        public IFormFile? BannerFile { get; set; }
    }
    // For organiser dashboard
    public class DashboardVm
    {
        public List<EventVm> Events { get; set; } = new();
        public Dictionary<int, int> EnrolmentCounts { get; set; } = new();
    }
}
