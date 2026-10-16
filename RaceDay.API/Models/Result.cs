using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Result
{
    [Key]
    public int ResultID { get; set; }

    public int EnrollmentID { get; set; }

    [Required]
    public TimeSpan FinishTime { get; set; }

    [Required]
    public int Position { get; set; }

    // Navigation
    public Enrollment Enrollment { get; set; } = null!;
}