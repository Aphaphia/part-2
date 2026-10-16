using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Auth;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using Xunit;

namespace RaceDay.API.Tests.Controllers;

public class AuthControllerTests
{
    // ============================================================
    // Helper: build a fully-wired AuthController with real services
    // and a fresh in-memory database.
    // ============================================================
    private static AuthController BuildController(RaceDayDbContext db, out ISessionService sessionService)
    {
        var hasher = new PasswordHasher();
        var jwt = new JwtService(TestConfiguration.Build());
        sessionService = new SessionService();

        var controller = new AuthController(db, hasher, jwt, sessionService);

        // Attach an HttpContext with a working session
        var httpContext = new DefaultHttpContext();
        httpContext.Session = TestSessionFactory.Build();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    // ============================================================
    // TEST 1: Register a brand-new user — should return 201 Created
    // ============================================================
    [Fact]
    public async Task Register_NewUser_ReturnsCreated_AndHashesPassword()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Register_NewUser_ReturnsCreated_AndHashesPassword));
        var controller = BuildController(db, out _);

        var request = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.new@example.com",
            Password = "Password123",
            Role = "Participant"
        };

        // ACT
        var result = await controller.Register(request);

        // ASSERT — response is 201
        var created = Assert.IsType<CreatedAtActionResult>(result);

        // ASSERT — the user was saved to the DB
        var saved = await db.Users.FirstOrDefaultAsync(u => u.Email == "john.new@example.com");
        Assert.NotNull(saved);
        Assert.Equal("John", saved!.FirstName);
        Assert.Equal("Participant", saved.Role);

        // ASSERT — the password was hashed (not stored in plain text)
        Assert.NotEqual("Password123", saved.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", saved.Password));
    }

    // ============================================================
    // TEST 2: Register duplicate email — should return 409 Conflict
    // ============================================================
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Register_DuplicateEmail_ReturnsConflict));

        // Manually insert an existing user — don't rely on HasData seed data
        db.Users.Add(new User
        {
            UserID = 1000,
            FirstName = "Existing",
            LastName = "User",
            Email = "duplicate@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Password123"),
            Role = "Participant"
        });
        await db.SaveChangesAsync();

        var controller = BuildController(db, out _);

        var request = new RegisterRequest
        {
            FirstName = "Another",
            LastName = "User",
            Email = "duplicate@example.com",   // same email as existing user
            Password = "Password123",
            Role = "Participant"
        };

        // ACT
        var result = await controller.Register(request);

        // ASSERT
        Assert.IsType<ConflictObjectResult>(result);
    }

    // ============================================================
    // TEST 3: Register with invalid role — should return 400 Bad Request
    // ============================================================
    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Register_InvalidRole_ReturnsBadRequest));
        var controller = BuildController(db, out _);

        var request = new RegisterRequest
        {
            FirstName = "Bad",
            LastName = "Role",
            Email = "bad.role@example.com",
            Password = "Password123",
            Role = "Admin"   // not allowed
        };

        // ACT
        var result = await controller.Register(request);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // TEST 4: Login with valid credentials — should return 200 OK + token
    // ============================================================
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Login_ValidCredentials_ReturnsOkWithToken));
        var controller = BuildController(db, out _);

        // Add a user we know the password for
        db.Users.Add(new User
        {
            UserID = 500,
            FirstName = "Login",
            LastName = "Test",
            Email = "login.test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Secret123"),
            Role = "Participant"
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "login.test@example.com",
            Password = "Secret123"
        };

        // ACT
        var result = await controller.Login(request);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        // Read the anonymous object via reflection (works because it's an AuthResponse DTO)
        var response = ok.Value!;
        var tokenProp = response.GetType().GetProperty("Token")!;
        var token = tokenProp.GetValue(response) as string;
        Assert.False(string.IsNullOrEmpty(token));
    }

    // ============================================================
    // TEST 5: Login with wrong password — should return 401
    // ============================================================
    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Login_WrongPassword_ReturnsUnauthorized));
        var controller = BuildController(db, out _);

        db.Users.Add(new User
        {
            UserID = 501,
            FirstName = "Wrong",
            LastName = "Pass",
            Email = "wrong.pass@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = "Participant"
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "wrong.pass@example.com",
            Password = "WrongPassword"
        };

        // ACT
        var result = await controller.Login(request);

        // ASSERT
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ============================================================
    // TEST 6: Login with unknown email — should return 401
    // ============================================================
    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Login_UnknownEmail_ReturnsUnauthorized));
        var controller = BuildController(db, out _);

        var request = new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Anything"
        };

        // ACT
        var result = await controller.Login(request);

        // ASSERT
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ============================================================
    // TEST 7: Login sets the session
    // ============================================================
    [Fact]
    public async Task Login_ValidCredentials_SetsSession()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Login_ValidCredentials_SetsSession));
        var controller = BuildController(db, out var sessionService);

        db.Users.Add(new User
        {
            UserID = 502,
            FirstName = "Session",
            LastName = "User",
            Email = "session@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("SessionPass"),
            Role = "Organiser"
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "session@example.com",
            Password = "SessionPass"
        };

        // ACT
        var result = await controller.Login(request);

        // ASSERT — response was OK
        Assert.IsType<OkObjectResult>(result);

        // ASSERT — session contains the user
        var storedUser = sessionService.GetUser(controller.HttpContext);
        Assert.NotNull(storedUser);
        Assert.Equal(502, storedUser!.UserID);
        Assert.Equal("Organiser", storedUser.Role);
        Assert.Equal("session@example.com", storedUser.Email);
    }
}