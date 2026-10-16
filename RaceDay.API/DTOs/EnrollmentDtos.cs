using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Enrollments;

public class EnrollmentDto
{
    public int EnrollmentID { get; set; }
    public int UserID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrollmentCreateDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

public class EnrollmentStatusUpdateDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // Confirmed | Cancelled
}
