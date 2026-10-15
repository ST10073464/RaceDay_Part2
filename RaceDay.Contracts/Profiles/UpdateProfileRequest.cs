using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Profiles;

public class UpdateProfileRequest
{
    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(1000), Url]
    public string? ProfilePictureUrl { get; set; }
}