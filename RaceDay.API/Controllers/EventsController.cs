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

        // Load organiser names in one query; leave event ownership unchanged.
        var organiserIds = events.Select(e => e.OrganiserId).Distinct().ToList();
        var organisers = await _context.Users
            .AsNoTracking()
            .Where(u => organiserIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FirstName, u.LastName, u.Email })
            .ToDictionaryAsync(u => u.UserId);

        var result = events.Select(e => new
        {
            e.EventId,
            e.EventName,
            e.Description,
            e.EventDate,
            e.Location,
            e.DistanceKm,
            e.EventType,

            BannerImageUrl = ResolveBannerUrlSafely(e.BannerImageUrl),

            e.OrganiserId,
            CreatedBy = organisers.TryGetValue(e.OrganiserId, out var organiser)
                ? (string.IsNullOrWhiteSpace(($"{organiser.FirstName} {organiser.LastName}").Trim())
                    ? organiser.Email
                    : ($"{organiser.FirstName} {organiser.LastName}").Trim())
                : "Unknown organiser"
        });

        return Ok(result);
    }

    // An invalid legacy banner URL must not prevent events from loading.
    // The stored value is left unchanged for later investigation.

    private string? ResolveBannerUrlSafely(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
            return null;

        if (!Uri.TryCreate(
                storedUrl.Trim(),
                UriKind.Absolute,
                out Uri? uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttps &&
            uri.Scheme != Uri.UriSchemeHttp)
            return null;

        // External website images do not need Azure SAS tokens.
        if (!uri.Host.EndsWith(
                ".blob.core.windows.net",
                StringComparison.OrdinalIgnoreCase))
        {
            return uri.ToString();
        }

        // Existing Azure-hosted banners still use SAS URLs.
        try
        {
            return _blobStorageService.GetEventBannerReadUrl(
                storedUrl);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (UriFormatException)
        {
            return null;
        }
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

        string? bannerImageUrl = ResolveBannerUrlSafely(existingEvent.BannerImageUrl);

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

        if (updatedEvent.BannerImageUrl != null)
        {
            existingEvent.BannerImageUrl =
                updatedEvent.BannerImageUrl;
        }

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
    // Uploads or replaces a banner image for an event
    // owned by the logged-in organiser.
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

        // Keep the existing banner URL so that it can
        // be deleted only after the new banner is saved.
        string? oldBannerImageUrl =
            existingEvent.BannerImageUrl;

        try
        {
            // Upload the new banner first.
            string bannerImageUrl =
                await _blobStorageService
                    .UploadEventBannerAsync(
                        bannerImage,
                        existingEvent.EventId);

            // Update the event to use the new banner.
            existingEvent.BannerImageUrl =
                bannerImageUrl;

            await _context.SaveChangesAsync();

            // Delete the old banner only after the new
            // banner has been uploaded and saved successfully.
            if (!string.IsNullOrWhiteSpace(
                    oldBannerImageUrl) &&
                !string.Equals(
                    oldBannerImageUrl,
                    bannerImageUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(
                        oldBannerImageUrl);
                }
                catch (Exception)
                {
                    // Do not fail the successful replacement
                    // if cleanup of the old blob fails.
                }
            }

            return Ok(new
            {
                message =
                    "Event banner uploaded successfully.",
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
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The event banner could not be uploaded."
                });
        }
    }

    // DELETE: api/events/{id}/banner
    // Removes either an external URL banner or an Azure-uploaded banner.
    [HttpDelete("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> RemoveEventBanner(int id)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var organiserId))
            return Unauthorized();

        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
            return NotFound(new { message = "Event not found." });

        if (existingEvent.OrganiserId != organiserId)
            return Forbid();

        string? existingBannerUrl = existingEvent.BannerImageUrl;
        if (string.IsNullOrWhiteSpace(existingBannerUrl))
            return BadRequest(new { message = "There is no event banner to remove." });

        // Only images stored in our Azure Blob Storage should be deleted there.
        // External HTTPS image URLs are references, not Azure blobs.
        bool isAzureBlob = Uri.TryCreate(existingBannerUrl, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase);

        try
        {
            if (isAzureBlob)
                await _blobStorageService.DeleteImageAsync(existingBannerUrl);

            existingEvent.BannerImageUrl = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event banner removed successfully." });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The event banner could not be removed." });
        }
    }

}
