namespace RaceDay.Mvc.Models
{
    public class OrganiserDashboardViewModel
    {
        public int TotalEvents { get; set; }

        public int UpcomingEvents { get; set; }

        public int TotalEnrolments { get; set; }

        public List<OrganiserDashboardEventViewModel> Events
        {
            get;
            set;
        } = new();
    }

    public class OrganiserDashboardEventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public int TotalEnrolments { get; set; }

        public bool IsUpcoming { get; set; }
    }
}