namespace RaceDay.Mvc.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateOnly EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        public decimal DistanceKm { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string? BannerImageUrl { get; set; }

        public int OrganiserId { get; set; }
    }
}