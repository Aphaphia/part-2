using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Results;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _current;

    public ResultsController(RaceDayDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    /// <summary>Lists results for the logged-in participant.</summary>
    [HttpGet("results/my")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults()
    {
        var list = await _db.Results
            .Include(r => r.Enrollment)
            .Where(r => r.Enrollment.UserID == _current.UserId)
            .Select(r => new ResultDto
            {
                ResultID = r.ResultID,
                EnrollmentID = r.EnrollmentID,
                FinishTime = r.FinishTime,
                Position = r.Position
            }).ToListAsync();

        return Ok(list);
    }

    /// <summary>Gets the result for a specific enrolment (owner or organiser).</summary>
    [HttpGet("enrollments/{enrollmentId:int}/result")]
    [Authorize]
    public async Task<IActionResult> GetForEnrollment(int enrollmentId)
    {
        var en = await _db.Enrollments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

        if (en is null) return NotFound();
        if (en.Result is null) return NotFound(new { message = "No result captured yet." });

        var isOwner = en.UserID == _current.UserId;
        var isOrganiser = _current.Role == "Organiser" && en.Event.OrganiserID == _current.UserId;
        if (!isOwner && !isOrganiser) return Forbid();

        return Ok(new ResultDto
        {
            ResultID = en.Result.ResultID,
            EnrollmentID = en.Result.EnrollmentID,
            FinishTime = en.Result.FinishTime,
            Position = en.Result.Position
        });
    }

    /// <summary>Captures a result (Organiser only).</summary>
    [HttpPost("results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] ResultCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var en = await _db.Enrollments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrollmentID == dto.EnrollmentId);
        if (en is null) return NotFound(new { message = "Enrollment not found." });
        if (en.Event.OrganiserID != _current.UserId) return Forbid();

        if (await _db.Results.AnyAsync(r => r.EnrollmentID == dto.EnrollmentId))
            return Conflict(new { message = "Result already exists for this enrollment." });

        var result = new Result
        {
            EnrollmentID = dto.EnrollmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetForEnrollment),
            new { enrollmentId = result.EnrollmentID }, new { result.ResultID });
    }

    /// <summary>Corrects an existing result (Organiser only).</summary>
    [HttpPut("results/{resultId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int resultId, [FromBody] ResultUpdateDto dto)
    {
        var r = await _db.Results
            .Include(x => x.Enrollment).ThenInclude(e => e.Event)
            .FirstOrDefaultAsync(x => x.ResultID == resultId);
        if (r is null) return NotFound();
        if (r.Enrollment.Event.OrganiserID != _current.UserId) return Forbid();

        r.FinishTime = dto.FinishTime;
        r.Position = dto.Position;
        await _db.SaveChangesAsync();

        return Ok(new ResultDto
        {
            ResultID = r.ResultID,
            EnrollmentID = r.EnrollmentID,
            FinishTime = r.FinishTime,
            Position = r.Position
        });
    }
}
