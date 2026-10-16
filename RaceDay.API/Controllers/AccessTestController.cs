
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/access-test")]
public class AccessTestController : ControllerBase
{
    [HttpGet("protected")]
    [Authorize]
    public IActionResult Protected()
    {
        return Ok(new
        {
            message = "Authentication successful.",
            user = User.Identity?.Name,
            role = User.FindFirst(
                System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [HttpGet("participant")]
    [Authorize(Roles = "Participant")]
    public IActionResult ParticipantOnly()
    {
        return Ok(new
        {
            message = "Participant access granted."
        });
    }

    [HttpGet("organiser")]
    [Authorize(Roles = "Organiser")]
    public IActionResult OrganiserOnly()
    {
        return Ok(new
        {
            message = "Organiser access granted."
        });
    }
}
