
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "date")]
    public DateOnly EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(6,2)")]
    public decimal DistanceKm { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? Organiser { get; set; }

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
