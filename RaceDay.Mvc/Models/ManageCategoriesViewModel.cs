using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class ManageCategoriesViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public List<CategoryViewModel> Categories { get; set; } = new();

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0, 120)]
        [Display(Name = "Minimum Age")]
        public int MinAge { get; set; }

        [Range(0, 120)]
        [Display(Name = "Maximum Age")]
        public int MaxAge { get; set; }

        [Range(0.01, 10000)]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }
    }
}