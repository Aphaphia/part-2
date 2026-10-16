using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _current;

    public EventsController(RaceDayDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    /// <summary>Lists all events (public).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .Select(e => new EventDto
            {
                EventID = e.EventID,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                Distance = e.Distance,
                EventType = e.EventType,
                OrganiserID = e.OrganiserID
            }).ToListAsync();

        return Ok(events);
    }

    /// <summary>Gets one event by ID (public).</summary>
    [HttpGet("{eventId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int eventId)
    {
        var e = await _db.Events.FindAsync(eventId);
        if (e is null) return NotFound();

        return Ok(new EventDto
        {
            EventID = e.EventID,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            OrganiserID = e.OrganiserID
        });
    }

    /// <summary>Creates a new event (Organiser only).</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create([FromBody] EventUpsertDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ev = new Event
        {
            EventName = dto.EventName,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            Distance = dto.Distance,
            EventType = dto.EventType,
            OrganiserID = _current.UserId!.Value
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { eventId = ev.EventID },
            new { ev.EventID });
    }

    /// <summary>Updates an event (owner Organiser only).</summary>
    [HttpPut("{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int eventId, [FromBody] EventUpsertDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != _current.UserId) return Forbid();

        ev.EventName = dto.EventName;
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location;
        ev.Distance = dto.Distance;
        ev.EventType = dto.EventType;

        await _db.SaveChangesAsync();
        return Ok(ev);
    }

    /// <summary>Deletes an event if no active enrolments exist.</summary>
    [HttpDelete("{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != _current.UserId) return Forbid();

        var hasActive = await _db.Enrollments
            .AnyAsync(e => e.EventID == eventId && e.Status == "Confirmed");
        if (hasActive) return Conflict(new { message = "Event has active enrolments." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
