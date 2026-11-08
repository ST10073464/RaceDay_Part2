using System.Collections.Generic;

namespace RaceDay.Mvc.Models
{
    public class EventCardViewModel
    {
        public EventViewModel Event { get; set; } = new();
        public List<CategoryViewModel> Categories { get; set; } = new();
        public int? ParticipantAgeOnEventDate { get; set; }

        public bool HasEligibleCategory => ParticipantAgeOnEventDate.HasValue &&
            Categories.Any(c => ParticipantAgeOnEventDate.Value >= c.MinAge &&
                                ParticipantAgeOnEventDate.Value <= c.MaxAge);
    }
}
