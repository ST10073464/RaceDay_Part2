using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
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
        var eventDetails = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventId == id)
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
                e.OrganiserId,
                Categories = e.Categories
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.MinAge,
                        c.MaxAge,
                        c.DistanceKm
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (eventDetails == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

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



}
