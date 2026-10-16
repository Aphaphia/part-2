using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Enrollments;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class EnrollmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _current;

    public EnrollmentsController(RaceDayDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    /// <summary>Lists the logged-in participant's enrolments.</summary>
    [HttpGet("enrollments/my")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyEnrollments()
    {
        var list = await _db.Enrollments
            .Where(e => e.UserID == _current.UserId)
            .Select(e => new EnrollmentDto
            {
                EnrollmentID = e.EnrollmentID,
                UserID = e.UserID,
                EventID = e.EventID,
                CategoryID = e.CategoryID,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status
            }).ToListAsync();

        return Ok(list);
    }

    /// <summary>Lists all enrolments for one of the organiser's events.</summary>
    [HttpGet("events/{eventId:int}/enrollments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != _current.UserId) return Forbid();

        var list = await _db.Enrollments
            .Where(e => e.EventID == eventId)
            .Select(e => new EnrollmentDto
            {
                EnrollmentID = e.EnrollmentID,
                UserID = e.UserID,
                EventID = e.EventID,
                CategoryID = e.CategoryID,
                EnrollmentDate = e.EnrollmentDate,
                Status = e.Status
            }).ToListAsync();

        return Ok(list);
    }

    /// <summary>Enrols the logged-in participant into an event/category.</summary>
    [HttpPost("enrollments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Create([FromBody] EnrollmentCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = await _db.Events.FindAsync(dto.EventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var cat = await _db.Categories.FindAsync(dto.CategoryId);
        if (cat is null || cat.EventID != dto.EventId)
            return BadRequest(new { message = "Category does not belong to this event." });

        var already = await _db.Enrollments
            .AnyAsync(e => e.UserID == _current.UserId && e.EventID == dto.EventId);
        if (already) return Conflict(new { message = "Already enrolled in this event." });

        var en = new Enrollment
        {
            UserID = _current.UserId!.Value,
            EventID = dto.EventId,
            CategoryID = dto.CategoryId,
            EnrollmentDate = DateTime.Now,
            Status = "Confirmed"
        };

        _db.Enrollments.Add(en);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(MyEnrollments), new { id = en.EnrollmentID },
            new { en.EnrollmentID });
    }
    /// <summary>Updates the status of an enrolment (owner Participant only).</summary>
    [HttpPut("enrollments/{enrollmentId:int}/status")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> UpdateStatus(int enrollmentId, [FromBody] EnrollmentStatusUpdateDto dto)
    {
        var en = await _db.Enrollments.FindAsync(enrollmentId);
        if (en is null) return NotFound();
        if (en.UserID != _current.UserId) return Forbid();
        if (dto.Status != "Confirmed" && dto.Status != "Cancelled")
            return BadRequest(new { message = "Status must be Confirmed or Cancelled." });

        en.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(new EnrollmentDto
        {
            EnrollmentID = en.EnrollmentID,
            UserID = en.UserID,
            EventID = en.EventID,
            CategoryID = en.CategoryID,
            EnrollmentDate = en.EnrollmentDate,
            Status = en.Status
        });
    }
}


