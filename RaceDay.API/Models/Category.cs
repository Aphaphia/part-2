using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models;

public class Category
{
    [Key]
    public int CategoryID { get; set; }

    public int EventID { get; set; }

    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
