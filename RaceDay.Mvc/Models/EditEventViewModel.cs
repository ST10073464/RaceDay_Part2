using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class EditEventViewModel
    {
        public int EventId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateOnly EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 10000)]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = string.Empty;

        [Display(Name = "Banner Image URL")]
        public string? BannerImageUrl { get; set; }
    }
}