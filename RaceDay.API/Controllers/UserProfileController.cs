using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.UserProfile;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _current;

    public UserProfileController(RaceDayDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    /// <summary>Gets the logged-in user's own profile.</summary>
    [HttpGet("{userId:int}/profile")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        if (_current.UserId != userId) return Forbid();

        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (profile is null) return NotFound();

        return Ok(new ProfileDto
        {
            ProfileID = profile.ProfileID,
            UserID = profile.UserID,
            DateOfBirth = profile.DateOfBirth,
            Phone = profile.Phone,
            City = profile.City
        });
    }

    /// <summary>Creates or updates the logged-in user's own profile.</summary>
    [HttpPut("{userId:int}/profile")]
    public async Task<IActionResult> UpsertProfile(int userId, [FromBody] ProfileUpsertDto dto)
    {
        if (_current.UserId != userId) return Forbid();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserID == userId);

        if (profile is null)
        {
            profile = new UserProfile
            {
                UserID = userId,
                DateOfBirth = dto.DateOfBirth,
                Phone = dto.Phone,
                City = dto.City
            };
            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.DateOfBirth = dto.DateOfBirth;
            profile.Phone = dto.Phone;
            profile.City = dto.City;
        }

        await _db.SaveChangesAsync();

        return Ok(new ProfileDto
        {
            ProfileID = profile.ProfileID,
            UserID = profile.UserID,
            DateOfBirth = profile.DateOfBirth,
            Phone = profile.Phone,
            City = profile.City
        });
    }
}