using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Results;

public class ResultDto
{
    public int ResultID { get; set; }
    public int EnrollmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}

public class ResultCreateDto
{
    [Required]
    public int EnrollmentId { get; set; }

    [Required]
    public TimeSpan FinishTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Position { get; set; }
}

public class ResultUpdateDto
{
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Position { get; set; }
}