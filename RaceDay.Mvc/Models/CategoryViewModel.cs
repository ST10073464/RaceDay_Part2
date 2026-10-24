namespace RaceDay.Mvc.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int MinAge { get; set; }

        public int MaxAge { get; set; }

        public decimal DistanceKm { get; set; }
    }
}