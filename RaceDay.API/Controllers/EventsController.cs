using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _blobStorageService;

    public EventsController(
    RaceDayDbContext context,
    BlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var result = events.Select(e => new
        {
            e.EventId,
            e.EventName,
            e.Description,
            e.EventDate,
            e.Location,
            e.DistanceKm,
            e.EventType,

            BannerImageUrl =
                string.IsNullOrWhiteSpace(e.BannerImageUrl)
                    ? null
                    : _blobStorageService.GetEventBannerReadUrl(
                        e.BannerImageUrl),

            e.OrganiserId
        });

        return Ok(result);
    }

    // GET: api/events/mine
    // Returns only events created by the logged-in organiser.
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetMyEvents()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var organiserId))
        {
            return Unauthorized();
        }

        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.EventName,
                e.Description,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                e.OrganiserId
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET: api/events/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        var existingEvent = await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        string? bannerImageUrl = null;

        if (!string.IsNullOrWhiteSpace(
            existingEvent.BannerImageUrl))
        {
            bannerImageUrl =
                _blobStorageService.GetEventBannerReadUrl(
                    existingEvent.BannerImageUrl);
        }

        var eventDetails = new
        {
            existingEvent.EventId,
            existingEvent.EventName,
            existingEvent.Description,
            existingEvent.EventDate,
            existingEvent.Location,
            existingEvent.DistanceKm,
            existingEvent.EventType,

            BannerImageUrl = bannerImageUrl,

            existingEvent.OrganiserId,

            Categories = existingEvent.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.MinAge,
                    c.MaxAge,
                    c.DistanceKm
                })
                .ToList()
        };

        return Ok(eventDetails);
    }

    // PUT: api/events/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] Event updatedEvent)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        if (existingEvent.OrganiserId != userId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(updatedEvent.EventName) ||
            string.IsNullOrWhiteSpace(updatedEvent.Location) ||
            updatedEvent.DistanceKm <= 0 ||
            updatedEvent.EventDate == default)
        {
            return BadRequest(new
            {
                message = "Event name, location, a valid date, and a positive distance are required."
            });
        }

        if (updatedEvent.EventType != "Run" &&
            updatedEvent.EventType != "Walk" &&
            updatedEvent.EventType != "Cycle")
        {
            return BadRequest(new
            {
                message = "Event type must be Run, Walk, or Cycle."
            });
        }

        existingEvent.EventName = updatedEvent.EventName.Trim();
        existingEvent.Description = updatedEvent.Description;
        existingEvent.EventDate = updatedEvent.EventDate;
        existingEvent.Location = updatedEvent.Location.Trim();
        existingEvent.DistanceKm = updatedEvent.DistanceKm;
        existingEvent.EventType = updatedEvent.EventType;
        existingEvent.BannerImageUrl = updatedEvent.BannerImageUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
        {
            return NotFound();
        }

        if (existingEvent.OrganiserId != userId)
        {
            return Forbid();
        }

        var hasEnrolments = await _context.Enrolments
            .AnyAsync(e => e.EventId == id);

        if (hasEnrolments)
        {
            return Conflict(new
            {
                message = "This event cannot be deleted because it has enrolments."
            });
        }

        var categories = await _context.Categories
            .Where(c => c.EventId == id)
            .ToListAsync();

        _context.Categories.RemoveRange(categories);
        _context.Events.Remove(existingEvent);

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // POST: api/events
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(
        [FromBody] Event newEvent)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(newEvent.EventName) ||
            string.IsNullOrWhiteSpace(newEvent.Location) ||
            newEvent.DistanceKm <= 0 ||
            newEvent.EventDate == default)
        {
            return BadRequest(new
            {
                message = "Event name, location, a valid date, and a positive distance are required."
            });
        }

        if (newEvent.EventType != "Run" &&
            newEvent.EventType != "Walk" &&
            newEvent.EventType != "Cycle")
        {
            return BadRequest(new
            {
                message = "Event type must be Run, Walk, or Cycle."
            });
        }

        newEvent.OrganiserId = userId;
        newEvent.CreatedAt = DateTime.UtcNow;

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvents),
            new { id = newEvent.EventId },
            new
            {
                newEvent.EventId,
                newEvent.EventName,
                newEvent.EventDate,
                newEvent.Location,
                newEvent.DistanceKm,
                newEvent.EventType,
                newEvent.OrganiserId
            });
    }

    // POST: api/events/{id}/banner
    // Uploads a banner image for an event owned by the logged-in organiser.
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadEventBanner(
        int id,
        IFormFile bannerImage)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var organiserId))
        {
            return Unauthorized();
        }

        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

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

        try
        {
            string bannerImageUrl =
                await _blobStorageService
                    .UploadEventBannerAsync(
                        bannerImage,
                        existingEvent.EventId);

            existingEvent.BannerImageUrl =
                bannerImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                bannerImageUrl
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
