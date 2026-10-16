using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/results")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST: api/results
        // Organiser records a result for a confirmed enrolment.
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateResult(Result result)
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var organiserId))
            {
                return Unauthorized();
            }

            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(
                    e => e.EnrolmentId == result.EnrolmentId);

            if (enrolment == null)
            {
                return NotFound(new
                {
                    message = "Enrolment not found."
                });
            }

            // Only the organiser who owns the event may record results.
            if (enrolment.Event.OrganiserId != organiserId)
            {
                return Forbid();
            }

            if (enrolment.EnrolmentStatus != "Confirmed")
            {
                return BadRequest(new
                {
                    message =
                        "Results can only be recorded for confirmed enrolments."
                });
            }

            if (result.FinishTime <= TimeSpan.Zero)
            {
                return BadRequest(new
                {
                    message = "Finish time must be greater than zero."
                });
            }

            if (result.FinishingPosition <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Finishing position must be greater than zero."
                });
            }

            var resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentId == result.EnrolmentId);

            if (resultExists)
            {
                return Conflict(new
                {
                    message =
                        "A result has already been recorded for this enrolment."
                });
            }

            result.ResultId = 0;
            result.PublishedAt = DateTime.UtcNow;

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyResults),
                new { },
                new
                {
                    result.ResultId,
                    result.EnrolmentId,
                    result.FinishTime,
                    result.FinishingPosition,
                    result.PublishedAt
                });
        }

        // GET: api/results/mine
        // Participant views only their own results.
        [HttpGet("mine")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var participantId))
            {
                return Unauthorized();
            }

            var results = await _context.Results
                .AsNoTracking()
                .Where(r =>
                    r.Enrolment.ParticipantId == participantId)
                .OrderByDescending(r => r.PublishedAt)
                .Select(r => new
                {
                    r.ResultId,
                    r.EnrolmentId,

                    EventId = r.Enrolment.EventId,
                    EventName = r.Enrolment.Event.EventName,

                    CategoryId = r.Enrolment.CategoryId,
                    CategoryName =
                        r.Enrolment.Category.CategoryName,

                    r.FinishTime,
                    r.FinishingPosition,
                    r.PublishedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/results/event/1
        // Organiser views results for an event they own.
        [HttpGet("event/{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

            var results = await _context.Results
                .AsNoTracking()
                .Where(r => r.Enrolment.EventId == eventId)
                .OrderBy(r => r.FinishingPosition)
                .Select(r => new
                {
                    r.ResultId,
                    r.EnrolmentId,

                    ParticipantId =
                        r.Enrolment.ParticipantId,

                    ParticipantName =
                        r.Enrolment.Participant.FirstName +
                        " " +
                        r.Enrolment.Participant.LastName,

                    CategoryName =
                        r.Enrolment.Category.CategoryName,

                    r.FinishTime,
                    r.FinishingPosition,
                    r.PublishedAt
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}