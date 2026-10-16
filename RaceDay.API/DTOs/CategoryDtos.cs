using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Categories;

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CategoryUpsertDto
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}