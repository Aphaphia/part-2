using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Controllers;

public class EventsControllerTests
{
    // ============================================================
    // Helper: build a controller with a fake logged-in user
    // ============================================================
    private static EventsController BuildController(RaceDayDbContext db, int? userId, string? role)
    {
        // Build a fake ICurrentUserService that returns the given user
        var httpContext = new DefaultHttpContext();

        if (userId.HasValue && role != null)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, authenticationType: "TestAuth");

            httpContext.User = new ClaimsPrincipal(identity);
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUser = new CurrentUserService(accessor);

        var controller = new EventsController(db, currentUser);

        // Also give the controller direct access to the same HttpContext
        // (used by User property on ControllerBase)
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    /// <summary>
    /// Helper: insert an event into the DB.
    /// </summary>
    private static async Task<Event> InsertEventAsync(RaceDayDbContext db, int organiserId, string name = "Test Event")
    {
        var ev = new Event
        {
            EventName = name,
            Description = "Description",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Test Location",
            Distance = 5.0m,
            EventType = "Run",
            OrganiserID = organiserId
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    // ============================================================
    // TEST 1: GET /api/events — public, returns all events
    // ============================================================
    [Fact]
    public async Task GetAll_ReturnsOkWithEvents()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetAll_ReturnsOkWithEvents));
        await InsertEventAsync(db, organiserId: 1, "Event A");
        await InsertEventAsync(db, organiserId: 1, "Event B");

        var controller = BuildController(db, userId: null, role: null);

        // ACT
        var result = await controller.GetAll();

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.NotNull(list);
    }

    // ============================================================
    // TEST 2: GET /api/events/{id} — public, valid ID
    // ============================================================
    [Fact]
    public async Task GetById_ValidId_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetById_ValidId_ReturnsOk));
        var ev = await InsertEventAsync(db, organiserId: 1, "Specific Event");

        var controller = BuildController(db, userId: null, role: null);

        // ACT
        var result = await controller.GetById(ev.EventID);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<EventDto>(ok.Value);
        Assert.Equal("Specific Event", dto.EventName);
        Assert.Equal(ev.EventID, dto.EventID);
    }

    // ============================================================
    // TEST 3: GET /api/events/{id} — public, invalid ID
    // ============================================================
    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetById_InvalidId_ReturnsNotFound));
        var controller = BuildController(db, userId: null, role: null);

        // ACT
        var result = await controller.GetById(9999);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }

    // ============================================================
    // TEST 4: POST /api/events — Organiser creates successfully
    // ============================================================
    [Fact]
    public async Task Create_AsOrganiser_ReturnsCreated()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_AsOrganiser_ReturnsCreated));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new EventUpsertDto
        {
            EventName = "New Marathon",
            Description = "A new event",
            EventDate = DateTime.UtcNow.AddDays(20),
            Location = "City Park",
            Distance = 42.0m,
            EventType = "Run"
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        var created = Assert.IsType<CreatedAtActionResult>(result);

        var saved = await db.Events.FirstOrDefaultAsync(e => e.EventName == "New Marathon");
        Assert.NotNull(saved);
        Assert.Equal(42, saved!.OrganiserID);   // owner is the logged-in organiser
    }

    // ============================================================
    // TEST 5: PUT /api/events/{id} — Organiser updates OWN event
    // ============================================================
    [Fact]
    public async Task Update_OwnEvent_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OwnEvent_ReturnsOk));
        var ev = await InsertEventAsync(db, organiserId: 42, "Original Name");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new EventUpsertDto
        {
            EventName = "Updated Name",
            Description = "Updated",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "New Location",
            Distance = 10.0m,
            EventType = "Walk"
        };

        // ACT
        var result = await controller.Update(ev.EventID, dto);

        // ASSERT
        Assert.IsType<OkObjectResult>(result);

        var updated = await db.Events.FindAsync(ev.EventID);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated!.EventName);
        Assert.Equal("Walk", updated.EventType);
    }

    // ============================================================
    // TEST 6: PUT /api/events/{id} — Organiser tries to update
    //         ANOTHER organiser's event → 403 Forbidden
    // ============================================================
    [Fact]
    public async Task Update_OtherOrganisersEvent_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OtherOrganisersEvent_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999, "Someone Else's Event");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new EventUpsertDto
        {
            EventName = "Hacked",
            EventDate = DateTime.UtcNow,
            Location = "X",
            Distance = 1m,
            EventType = "Run"
        };

        // ACT
        var result = await controller.Update(ev.EventID, dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 7: PUT /api/events/{id} — Event does not exist
    // ============================================================
    [Fact]
    public async Task Update_NonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_NonExistentEvent_ReturnsNotFound));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new EventUpsertDto
        {
            EventName = "Nope",
            EventDate = DateTime.UtcNow,
            Location = "X",
            Distance = 1m,
            EventType = "Run"
        };

        // ACT
        var result = await controller.Update(9999, dto);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }

    // ============================================================
    // TEST 8: DELETE /api/events/{id} — Owner deletes event
    //         with no enrolments → 204
    // ============================================================
    [Fact]
    public async Task Delete_OwnEventWithoutEnrollments_ReturnsNoContent()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_OwnEventWithoutEnrollments_ReturnsNoContent));
        var ev = await InsertEventAsync(db, organiserId: 42, "To Be Deleted");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(ev.EventID);

        // ASSERT
        Assert.IsType<NoContentResult>(result);

        var stillThere = await db.Events.FindAsync(ev.EventID);
        Assert.Null(stillThere);
    }

    // ============================================================
    // TEST 9: DELETE /api/events/{id} — Event has confirmed
    //         enrolments → 409 Conflict
    // ============================================================
    [Fact]
    public async Task Delete_EventWithActiveEnrollments_ReturnsConflict()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_EventWithActiveEnrollments_ReturnsConflict));
        var ev = await InsertEventAsync(db, organiserId: 42, "Has Enrolments");

        // Add a category and enrollment
        var category = new Category { EventID = ev.EventID, CategoryName = "Open" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Enrollments.Add(new Enrollment
        {
            UserID = 100,
            EventID = ev.EventID,
            CategoryID = category.CategoryID,
            Status = "Confirmed"
        });
        await db.SaveChangesAsync();

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(ev.EventID);

        // ASSERT
        Assert.IsType<ConflictObjectResult>(result);
    }

    // ============================================================
    // TEST 10: DELETE /api/events/{id} — Organiser tries to
    //          delete ANOTHER organiser's event → 403
    // ============================================================
    [Fact]
    public async Task Delete_OtherOrganisersEvent_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_OtherOrganisersEvent_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999, "Not Mine");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(ev.EventID);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }
}