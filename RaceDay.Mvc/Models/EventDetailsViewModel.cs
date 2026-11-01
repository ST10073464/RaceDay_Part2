namespace RaceDay.Mvc.Models
{
    public class EventDetailsViewModel
    {
        public EventViewModel Event { get; set; } = new();

        public List<CategoryViewModel> Categories { get; set; } = new();

        public EnrolmentViewModel? ExistingEnrolment { get; set; }

        // Participant's age calculated on the event date.
        public int? ParticipantAgeOnEventDate { get; set; }
    }
}