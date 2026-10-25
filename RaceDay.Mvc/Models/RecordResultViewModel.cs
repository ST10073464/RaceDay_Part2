using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class RecordResultViewModel
    {
        public int EnrolmentId { get; set; }

        public int EventId { get; set; }

        public string EventName { get; set; } =
            string.Empty;

        public string ParticipantName { get; set; } =
            string.Empty;

        public string CategoryName { get; set; } =
            string.Empty;

        [Required]
        [Display(Name = "Finish Time")]
        public TimeSpan FinishTime { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Finishing position must be greater than zero.")]
        [Display(Name = "Finishing Position")]
        public int FinishingPosition { get; set; }
    }
}