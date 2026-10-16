using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Event
{
    [Key]
    public int EventID { get; set; }

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public decimal Distance { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle

    public int OrganiserID { get; set; }

    // Navigation
    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
