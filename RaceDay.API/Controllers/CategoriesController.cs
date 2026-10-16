using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Categories;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _current;

    public CategoriesController(RaceDayDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    /// <summary>Lists categories for an event (public).</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound();

        var list = await _db.Categories
            .Where(c => c.EventID == eventId)
            .Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                CategoryName = c.CategoryName,
                Description = c.Description
            }).ToListAsync();

        return Ok(list);
    }

    /// <summary>Creates a category (owner Organiser only).</summary>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryUpsertDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != _current.UserId) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var cat = new Category
        {
            EventID = eventId,
            CategoryName = dto.CategoryName,
            Description = dto.Description
        };

        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetForEvent), new { eventId }, new { cat.CategoryID });
    }

    /// <summary>Updates a category (Organiser only).</summary>
    [HttpPut("categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryUpsertDto dto)
    {
        var cat = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserID != _current.UserId) return Forbid();

        cat.CategoryName = dto.CategoryName;
        cat.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Ok(cat);
    }

    /// <summary>Deletes a category if no enrolments reference it.</summary>
    [HttpDelete("categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var cat = await _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserID != _current.UserId) return Forbid();

        var inUse = await _db.Enrollments.AnyAsync(e => e.CategoryID == categoryId);
        if (inUse) return Conflict(new { message = "Category has existing enrolments." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}