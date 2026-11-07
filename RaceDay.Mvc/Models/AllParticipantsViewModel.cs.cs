using System.Collections.Generic;

namespace RaceDay.Mvc.Models
{
    public class AllParticipantsViewModel
    {
        public int TotalEvents { get; set; }

        public List<AllParticipantEnrolmentViewModel> Enrolments { get; set; }
            = new();
    }

    public class AllParticipantEnrolmentViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public EventEnrolmentViewModel Enrolment { get; set; } = new();
    }
}