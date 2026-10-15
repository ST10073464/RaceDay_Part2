namespace RaceDay.Contracts.Profiles;

public class ProfileResponse
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}