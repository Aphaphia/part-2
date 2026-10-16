using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class UserProfile
{
    [Key]
    public int ProfileID { get; set; }

    public int UserID { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}