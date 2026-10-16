
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Contracts.Profiles;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
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
}
