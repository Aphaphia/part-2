using Microsoft.AspNetCore.Http;
using RaceDay.API.Services;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Services;

public class CurrentUserServiceTests
{
    
    private static CurrentUserService BuildService(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new CurrentUserService(accessor);
    }

    // ============================================================
    // TEST 1: When a NameIdentifier claim is present, UserId is returned
    // ============================================================
    [Fact]
    public void UserId_WithValidClaim_ReturnsUserId()
    {
        // ARRANGE
        var service = BuildService(
            new Claim(ClaimTypes.NameIdentifier, "42"));

        // ACT
        var userId = service.UserId;

        // ASSERT
        Assert.Equal(42, userId);
    }

    // ============================================================
    // TEST 2: When the claim is missing, UserId returns null
    // ============================================================
    [Fact]
    public void UserId_WithNoClaim_ReturnsNull()
    {
        // ARRANGE — empty claims list
        var service = BuildService();

        // ACT
        var userId = service.UserId;

        // ASSERT
        Assert.Null(userId);
    }

    // ============================================================
    // TEST 3: When the claim has garbage data, UserId returns null
    //         (this protects against malformed tokens)
    // ============================================================
    [Fact]
    public void UserId_WithNonNumericClaim_ReturnsNull()
    {
        // ARRANGE
        var service = BuildService(
            new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        // ACT
        var userId = service.UserId;

        // ASSERT
        Assert.Null(userId);
    }

    // ============================================================
    // TEST 4: Role claim is returned correctly
    // ============================================================
    [Fact]
    public void Role_WithValidClaim_ReturnsRole()
    {
        // ARRANGE
        var service = BuildService(
            new Claim(ClaimTypes.Role, "Organiser"));

        // ACT
        var role = service.Role;

        // ASSERT
        Assert.Equal("Organiser", role);
    }

    // ============================================================
    // TEST 5: Role is null when claim is missing
    // ============================================================
    [Fact]
    public void Role_WithNoClaim_ReturnsNull()
    {
        // ARRANGE
        var service = BuildService();

        // ACT
        var role = service.Role;

        // ASSERT
        Assert.Null(role);
    }

    // ============================================================
    // TEST 6: Both UserId and Role are read from the same context
    // ============================================================
    [Fact]
    public void UserIdAndRole_BothPresent_ReturnsBothCorrectly()
    {
        // ARRANGE
        var service = BuildService(
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, "Participant"),
            new Claim(ClaimTypes.Email, "someone@test.com"));

        // ACT + ASSERT
        Assert.Equal(7, service.UserId);
        Assert.Equal("Participant", service.Role);
    }
}