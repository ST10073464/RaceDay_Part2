namespace RaceDay.Mvc.Models
{
    public class ResultViewModel
    {
        public int ResultId { get; set; }

        public int EnrolmentId { get; set; }

        public int EventId { get; set; }

        public string EventName { get; set; } =
            string.Empty;

        public int ParticipantId { get; set; }

        public string ParticipantName { get; set; } =
            string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } =
            string.Empty;

        public TimeSpan FinishTime { get; set; }

        public DateTime PublishedAt { get; set; }
    }
}