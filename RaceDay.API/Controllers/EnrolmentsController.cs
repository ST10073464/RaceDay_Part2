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
        public async Task<IActionResult> CreateEnrolment(Enrolment enrolment)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var participantId))
            {
                return Unauthorized();
            }

            var existingEvent = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EventId == enrolment.EventId);

            if (existingEvent == null)
            {
                return NotFound(new
                {
                    message = "Event not found."
                });
            }

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == enrolment.CategoryId);

            if (category == null)
            {
                return NotFound(new
                {
                    message = "Category not found."
                });
            }

            // The selected category must belong to the selected event.
            if (category.EventId != enrolment.EventId)
            {
                return BadRequest(new
                {
                    message = "The selected category does not belong to this event."
                });
            }

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantId == participantId &&
                    e.EventId == enrolment.EventId);

            if (alreadyEnrolled)
            {
                return Conflict(new
                {
                    message = "You are already enrolled in this event."
                });
            }

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