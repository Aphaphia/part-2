using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Enrollment
{
    [Key]
    public int EnrollmentID { get; set; }

    public int UserID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }

    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed"; // Confirmed | Cancelled

    // Navigation
    public User User { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}