
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    [Column(TypeName = "time")]
    public TimeSpan FinishTime { get; set; }

    public int FinishingPosition { get; set; }

    public DateTime? PublishedAt { get; set; }

    public Enrolment? Enrolment { get; set; }
}
