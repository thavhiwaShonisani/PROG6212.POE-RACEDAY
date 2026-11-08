using System.Text.Json.Serialization;

namespace RaceDayManagementAPI.Models
{
    public class Enrolment
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Active";

        public User? Participant { get; set; }
        public Category? Category { get; set; }

        [JsonIgnore] public Result? Result { get; set; }    
    }
}
