using Microsoft.AspNetCore.Http;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using Xunit;

namespace RaceDay.API.Tests.Services;

public class SessionServiceTests
{
    /// <summary>
    /// Helper: creates a real HttpContext with a working in-memory session.
    /// </summary>
    private static DefaultHttpContext BuildContextWithSession()
    {
        var httpContext = new DefaultHttpContext
        {
            Session = TestSessionFactory.Build()
        };
        return httpContext;
    }

    // ============================================================
    // TEST 1: Set then Get returns the same user
    // ============================================================
    [Fact]
    public void SetUser_ThenGetUser_ReturnsSameUser()
    {
        // ARRANGE
        var service = new SessionService();
        var ctx = BuildContextWithSession();

        var user = new SessionUser
        {
            UserID = 42,
            Email = "test@example.com",
            Role = "Organiser"
        };

        // ACT
        service.SetUser(ctx, user);
        var retrieved = service.GetUser(ctx);

        // ASSERT
        Assert.NotNull(retrieved);
        Assert.Equal(42, retrieved!.UserID);
        Assert.Equal("test@example.com", retrieved.Email);
        Assert.Equal("Organiser", retrieved.Role);
    }

    // ============================================================
    // TEST 2: Get returns null when nothing was set
    // ============================================================
    [Fact]
    public void GetUser_WithNothingSet_ReturnsNull()
    {
        // ARRANGE
        var service = new SessionService();
        var ctx = BuildContextWithSession();

        // ACT
        var retrieved = service.GetUser(ctx);

        // ASSERT
        Assert.Null(retrieved);
    }

    // ============================================================
    // TEST 3: Clear removes the stored user
    // ============================================================
    [Fact]
    public void Clear_RemovesStoredUser()
    {
        // ARRANGE
        var service = new SessionService();
        var ctx = BuildContextWithSession();

        service.SetUser(ctx, new SessionUser
        {
            UserID = 1,
            Email = "a@a.com",
            Role = "Participant"
        });

        // ACT
        service.Clear(ctx);
        var retrieved = service.GetUser(ctx);

        // ASSERT
        Assert.Null(retrieved);
    }

    // ============================================================
    // TEST 4: Two different contexts have independent sessions
    // ============================================================
    [Fact]
    public void TwoDifferentContexts_HaveIndependentSessions()
    {
        // ARRANGE
        var service = new SessionService();
        var ctxA = BuildContextWithSession();
        var ctxB = BuildContextWithSession();

        service.SetUser(ctxA, new SessionUser { UserID = 1, Email = "a@a.com", Role = "Organiser" });

        // ACT
        var userA = service.GetUser(ctxA);
        var userB = service.GetUser(ctxB);

        // ASSERT
        Assert.NotNull(userA);
        Assert.Null(userB);
    }

    // ============================================================
    // TEST 5: Set replaces the previous user
    // ============================================================
    [Fact]
    public void SetUser_OverwritesPreviousUser()
    {
        // ARRANGE
        var service = new SessionService();
        var ctx = BuildContextWithSession();

        service.SetUser(ctx, new SessionUser { UserID = 1, Email = "first@test.com", Role = "Participant" });

        // ACT
        service.SetUser(ctx, new SessionUser { UserID = 2, Email = "second@test.com", Role = "Organiser" });
        var retrieved = service.GetUser(ctx);

        // ASSERT
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.UserID);
        Assert.Equal("second@test.com", retrieved.Email);
        Assert.Equal("Organiser", retrieved.Role);
    }
}