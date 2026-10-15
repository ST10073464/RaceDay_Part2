
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; }

    [Required, MaxLength(20)]
    public string EnrolmentStatus { get; set; } = "Confirmed";

    public User? Participant { get; set; }

    public Event? Event { get; set; }

    public Category? Category { get; set; }

    public Result? Result { get; set; }
}
