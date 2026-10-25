namespace RaceDay.Mvc.Models
{
    public class EventParticipantsViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } =
            string.Empty;

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } =
            string.Empty;

        public List<EventEnrolmentViewModel> Enrolments
        {
            get;
            set;
        } = new();
    }
}