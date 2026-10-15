
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [Column(TypeName = "date")]
    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(1000)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public Role? Role { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; }
        = new List<Event>();

    public ICollection<Enrolment> Enrolments { get; set; }
        = new List<Enrolment>();
}
