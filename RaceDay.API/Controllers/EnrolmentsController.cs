using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST: api/enrolments
        // Participants enrol themselves into an event/category.
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(
            Enrolment enrolment)
        {
            var userIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(
                userIdClaim,
                out var participantId))
            {
                return Unauthorized();
            }

            // -------------------------------------------------
            // VALIDATE EVENT
            // -------------------------------------------------

            var existingEvent =
                await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        e => e.EventId ==
                             enrolment.EventId);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            DateOnly today =
                DateOnly.FromDateTime(
                    DateTime.Today);

            if (existingEvent.EventDate < today)
            {
                return BadRequest(new
                {
                    message =
                        "You cannot enrol in an event " +
                        "that has already taken place."
                });
            }

            // -------------------------------------------------
            // VALIDATE CATEGORY
            // -------------------------------------------------

            var category =
                await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.CategoryId ==
                             enrolment.CategoryId);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            if (category.EventId != enrolment.EventId)
            {
                return BadRequest(new
                {
                    message =
                        "The selected category does not " +
                        "belong to this event."
                });
            }

            // -------------------------------------------------
            // VALIDATE PARTICIPANT PROFILE
            // -------------------------------------------------

            var participant =
                await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserId == participantId);

            if (participant == null)
            {
                return NotFound(new
                {
                    message =
                        "Participant profile could not be found."
                });
            }

            if (!participant.DateOfBirth.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "Please add your date of birth to " +
                        "your profile before enrolling in " +
                        "an event."
                });
            }

            // -------------------------------------------------
            // CALCULATE AGE ON EVENT DATE
            // -------------------------------------------------

            DateOnly dateOfBirth =
                participant.DateOfBirth.Value;

            DateOnly eventDate =
                existingEvent.EventDate;

            int age =
                eventDate.Year -
                dateOfBirth.Year;

            if (eventDate <
                dateOfBirth.AddYears(age))
            {
                age--;
            }

            // -------------------------------------------------
            // VALIDATE CATEGORY AGE
            // -------------------------------------------------

            if (age < category.MinAge ||
                age > category.MaxAge)
            {
                return BadRequest(new
                {
                    message =
                        $"You must be between " +
                        $"{category.MinAge} and " +
                        $"{category.MaxAge} years old " +
                        $"on the event date to enrol in " +
                        $"{category.CategoryName}. " +
                        $"Your age on the event date " +
                        $"will be {age}."
                });
            }

            // -------------------------------------------------
            // CHECK EXISTING ENROLMENT
            // -------------------------------------------------

            var existingEnrolment =
                await _context.Enrolments
                    .FirstOrDefaultAsync(
                        e =>
                            e.ParticipantId == participantId &&
                            e.EventId == enrolment.EventId);

            if (existingEnrolment != null)
            {
                // A cancelled enrolment may be reactivated.
                if (string.Equals(
                    existingEnrolment.EnrolmentStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
                {
                    existingEnrolment.CategoryId =
                        enrolment.CategoryId;

                    existingEnrolment.EnrolmentDate =
                        DateTime.UtcNow;

                    existingEnrolment.EnrolmentStatus =
                        "Confirmed";

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message =
                            "You have successfully re-enrolled in this event.",

                        existingEnrolment.EnrolmentId,
                        existingEnrolment.ParticipantId,
                        existingEnrolment.EventId,
                        existingEnrolment.CategoryId,
                        existingEnrolment.EnrolmentDate,
                        existingEnrolment.EnrolmentStatus
                    });
                }

                return Conflict(new
                {
                    message =
                        "You are already enrolled in this event."
                });
            }

            // -------------------------------------------------
            // CREATE ENROLMENT
            // -------------------------------------------------

            enrolment.EnrolmentId = 0;
            enrolment.ParticipantId = participantId;
            enrolment.EnrolmentDate = DateTime.UtcNow;
            enrolment.EnrolmentStatus = "Confirmed";

            _context.Enrolments.Add(enrolment);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyEnrolments),
                new
                {
                    enrolment.EnrolmentId
                },
                new
                {
                    enrolment.EnrolmentId,
                    enrolment.ParticipantId,
                    enrolment.EventId,
                    enrolment.CategoryId,
                    enrolment.EnrolmentDate,
                    enrolment.EnrolmentStatus
                });
        }

        // GET: api/enrolments/mine
        // Participant views only their own enrolments.
        [HttpGet("mine")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var participantId))
            {
                return Unauthorized();
            }

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.ParticipantId == participantId)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.EventId,
                    EventName = e.Event.EventName,
                    e.CategoryId,
                    CategoryName = e.Category.CategoryName,
                    e.EnrolmentDate,
                    e.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // GET: api/enrolments/event/1
        // Only the organiser who owns the event can view its enrolments.
        [HttpGet("event/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var organiserId))
            {
                return Unauthorized();
            }

            var existingEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            if (existingEvent.OrganiserId != organiserId)
            {
                return Forbid();
            }

            var enrolments = await _context.Enrolments
                .AsNoTracking()
                .Where(e => e.EventId == eventId)
                .OrderBy(e => e.EnrolmentId)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.ParticipantId,
                    ParticipantName =
                        e.Participant.FirstName + " " +
                        e.Participant.LastName,
                    e.CategoryId,
                    CategoryName = e.Category.CategoryName,
                    e.EnrolmentDate,
                    e.EnrolmentStatus
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // DELETE: api/enrolments/1
        // Participant cancels their own enrolment.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var participantId))
            {
                return Unauthorized();
            }

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentId == id);

            if (enrolment == null)
            {
                return NotFound();
            }

            if (enrolment.ParticipantId != participantId)
            {
                return Forbid();
            }

            // Keep the record for history instead of physically deleting it.
            enrolment.EnrolmentStatus = "Cancelled";

            await _context.SaveChangesAsync();

            return NoContent();
        }

    }

}