
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public int MinAge { get; set; }

    public int MaxAge { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal DistanceKm { get; set; }

    public Event? Event { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
