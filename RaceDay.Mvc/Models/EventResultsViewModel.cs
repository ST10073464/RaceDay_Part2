namespace RaceDay.Mvc.Models
{
    public class EventResultsViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } =
            string.Empty;

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } =
            string.Empty;

        public List<ResultViewModel> Results
        {
            get;
            set;
        } = new();
    }
}