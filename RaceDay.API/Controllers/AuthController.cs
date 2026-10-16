using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Auth;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly ISessionService _session;

    public AuthController(RaceDayDbContext db, IPasswordHasher hasher,
        IJwtService jwt, ISessionService session)
    {
        _db = db; _hasher = hasher; _jwt = jwt; _session = session;
    }

    /// <summary>Registers a new user as Organiser or Participant.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (req.Role != "Organiser" && req.Role != "Participant")
            return BadRequest(new { message = "Role must be Organiser or Participant." });

        var exists = await _db.Users.AnyAsync(u => u.Email == req.Email);
        if (exists) return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Password = _hasher.Hash(req.Password),
            Role = req.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new { id = user.UserID },
            new { user.UserID, user.FirstName, user.LastName, user.Email, user.Role });
    }

    /// <summary>Logs in a user and returns a JWT plus a server-side session.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null || !_hasher.Verify(req.Password, user.Password))
            return Unauthorized(new { message = "Incorrect email or password." });

        var token = _jwt.GenerateToken(user);

        _session.SetUser(HttpContext, new SessionUser
        {
            UserID = user.UserID,
            Role = user.Role,
            Email = user.Email
        });

        return Ok(new AuthResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Token = token
        });
    }

    /// <summary>Clears the server-side session.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _session.Clear(HttpContext);
        return NoContent();
    }
}
