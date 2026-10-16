using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Events;

public class EventDto
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserID { get; set; }
}

public class EventUpsertDto
{
    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.01, 999.99)]
    public decimal Distance { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty;
}
