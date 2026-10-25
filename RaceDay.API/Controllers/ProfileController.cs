
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts.Profiles;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _blobStorageService;

    public ProfileController(
    RaceDayDbContext context,
    BlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    // GET: api/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new ProfileResponse
            {
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                DateOfBirth = u.DateOfBirth,
                ProfilePictureUrl = u.ProfilePictureUrl
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(
            user.ProfilePictureUrl))
        {
            try
            {
                user.ProfilePictureUrl =
                    _blobStorageService
                        .GetProfilePictureReadUrl(
                            user.ProfilePictureUrl);
            }
            catch (Exception)
            {
                user.ProfilePictureUrl = null;
            }
        }

        return Ok(user);
    }

    // PUT: api/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new
            {
                message = "First name and last name are required."
            });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/profile/picture
    [HttpPost("picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePicture(
        IFormFile profilePicture)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        if (profilePicture == null ||
            profilePicture.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a profile picture."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User account was not found."
            });
        }

        // Keep the existing picture URL so that it can
        // be deleted only after the new picture is saved.
        string? oldProfilePictureUrl =
            user.ProfilePictureUrl;

        try
        {
            // Upload the new picture first.
            string profilePictureUrl =
                await _blobStorageService.UploadProfilePictureAsync(
                    profilePicture,
                    userId);

            // Update the database to use the new picture.
            user.ProfilePictureUrl =
                profilePictureUrl;

            await _context.SaveChangesAsync();

            // Only after the new picture has been uploaded
            // and saved successfully do we remove the old one.
            if (!string.IsNullOrWhiteSpace(
                    oldProfilePictureUrl) &&
                !string.Equals(
                    oldProfilePictureUrl,
                    profilePictureUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _blobStorageService.DeleteImageAsync(
                        oldProfilePictureUrl);
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
                    "Profile picture uploaded successfully.",
                profilePictureUrl
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
                        "The profile picture could not be uploaded."
                });
        }
    }

    // DELETE: api/profile/picture
    [HttpDelete("picture")]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User account was not found."
            });
        }

        if (string.IsNullOrWhiteSpace(
            user.ProfilePictureUrl))
        {
            return BadRequest(new
            {
                message = "There is no profile picture to remove."
            });
        }

        string existingPictureUrl =
            user.ProfilePictureUrl;

        try
        {
            await _blobStorageService.DeleteImageAsync(
                existingPictureUrl);

            user.ProfilePictureUrl = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture removed successfully."
            });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The profile picture could not be removed."
                });
        }
    }

}

