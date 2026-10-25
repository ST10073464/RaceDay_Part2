
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
        user.ProfilePictureUrl = request.ProfilePictureUrl;

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

        try
        {
            string profilePictureUrl =
                await _blobStorageService.UploadProfilePictureAsync(
                    profilePicture,
                    userId);

            user.ProfilePictureUrl = profilePictureUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
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

}

