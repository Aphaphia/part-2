namespace RaceDay.API.DTOs.UserProfile;

public class ProfileDto
{
    public int ProfileID { get; set; }
    public int UserID { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
}

public class ProfileUpsertDto
{
    public DateTime DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
}