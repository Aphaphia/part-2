using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Enrollments;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Controllers;

public class EnrollmentsControllerTests
{
    // ============================================================
    // Helper: build a controller with a fake logged-in user
    // ============================================================
    private static EnrollmentsController BuildController(RaceDayDbContext db, int? userId, string? role)
    {
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

        var controller = new EnrollmentsController(db, currentUser)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return controller;
    }

    /// <summary>Helper: insert an event owned by organiserId.</summary>
    private static async Task<Event> InsertEventAsync(RaceDayDbContext db, int organiserId)
    {
        var ev = new Event
        {
            EventName = "Test Event",
            Description = "Desc",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Location",
            Distance = 5m,
            EventType = "Run",
            OrganiserID = organiserId
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    /// <summary>Helper: insert a category under an event.</summary>
    private static async Task<Category> InsertCategoryAsync(RaceDayDbContext db, int eventId, string name = "Open")
    {
        var cat = new Category
        {
            EventID = eventId,
            CategoryName = name,
            Description = "Description"
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    /// <summary>Helper: insert an enrollment.</summary>
    private static async Task<Enrollment> InsertEnrollmentAsync(RaceDayDbContext db, int userId, int eventId, int categoryId, string status = "Confirmed")
    {
        var en = new Enrollment
        {
            UserID = userId,
            EventID = eventId,
            CategoryID = categoryId,
            EnrollmentDate = DateTime.Now,
            Status = status
        };
        db.Enrollments.Add(en);
        await db.SaveChangesAsync();
        return en;
    }

    // ============================================================
    // TEST 1: Participant sees only their OWN enrolments
    // ============================================================
    [Fact]
    public async Task MyEnrollments_ReturnsOnlyOwnEnrollments()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(MyEnrollments_ReturnsOnlyOwnEnrollments));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);

        // Participant 10 has 1 enrolment; Participant 20 has 1 enrolment
        await InsertEnrollmentAsync(db, userId: 10, eventId: ev.EventID, categoryId: cat.CategoryID);

        var ev2 = await InsertEventAsync(db, organiserId: 1);
        var cat2 = await InsertCategoryAsync(db, ev2.EventID);
        await InsertEnrollmentAsync(db, userId: 20, eventId: ev2.EventID, categoryId: cat2.CategoryID);

        // Act as participant 10
        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.MyEnrollments();

        // ASSERT — should return OK with exactly 1 enrollment (user 10's)
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<EnrollmentDto>().ToList();
        Assert.Single(list);
        Assert.Equal(10, list[0].UserID);
    }

    // ============================================================
    // TEST 2: Participant with no enrolments gets empty list
    // ============================================================
    [Fact]
    public async Task MyEnrollments_NoEnrollments_ReturnsEmptyList()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(MyEnrollments_NoEnrollments_ReturnsEmptyList));
        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.MyEnrollments();

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<EnrollmentDto>().ToList();
        Assert.Empty(list);
    }

    // ============================================================
    // TEST 3: Organiser views enrolments of their OWN event
    // ============================================================
    [Fact]
    public async Task ForEvent_OwnerOrganiser_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(ForEvent_OwnerOrganiser_ReturnsOk));
        var ev = await InsertEventAsync(db, organiserId: 42);
        var cat = await InsertCategoryAsync(db, ev.EventID);
        await InsertEnrollmentAsync(db, userId: 10, eventId: ev.EventID, categoryId: cat.CategoryID);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.ForEvent(ev.EventID);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value).Cast<EnrollmentDto>().ToList();
        Assert.Single(list);
    }

    // ============================================================
    // TEST 4: Organiser views another organiser's event → 403
    // ============================================================
    [Fact]
    public async Task ForEvent_OtherOrganisersEvent_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(ForEvent_OtherOrganisersEvent_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.ForEvent(ev.EventID);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 5: Organiser views a non-existent event → 404
    // ============================================================
    [Fact]
    public async Task ForEvent_NonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(ForEvent_NonExistentEvent_ReturnsNotFound));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.ForEvent(9999);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }

    // ============================================================
    // TEST 6: Participant enrols successfully → 201 Created
    // ============================================================
    [Fact]
    public async Task Create_ValidParticipant_ReturnsCreated()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_ValidParticipant_ReturnsCreated));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentCreateDto
        {
            EventId = ev.EventID,
            CategoryId = cat.CategoryID
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<CreatedAtActionResult>(result);

        var saved = await db.Enrollments.FirstOrDefaultAsync(e => e.UserID == 10 && e.EventID == ev.EventID);
        Assert.NotNull(saved);
        Assert.Equal("Confirmed", saved!.Status);
    }

    // ============================================================
    // TEST 7: Participant can't enrol twice → 409 Conflict
    // ============================================================
    [Fact]
    public async Task Create_DuplicateEnrollment_ReturnsConflict()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_DuplicateEnrollment_ReturnsConflict));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);

        // Pre-insert an enrollment for user 10
        await InsertEnrollmentAsync(db, userId: 10, eventId: ev.EventID, categoryId: cat.CategoryID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentCreateDto
        {
            EventId = ev.EventID,
            CategoryId = cat.CategoryID
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<ConflictObjectResult>(result);
    }

    // ============================================================
    // TEST 8: Category doesn't belong to event → 400 Bad Request
    // ============================================================
    [Fact]
    public async Task Create_CategoryNotInEvent_ReturnsBadRequest()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_CategoryNotInEvent_ReturnsBadRequest));
        var ev1 = await InsertEventAsync(db, organiserId: 1);
        var ev2 = await InsertEventAsync(db, organiserId: 1);
        var cat2 = await InsertCategoryAsync(db, ev2.EventID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentCreateDto
        {
            EventId = ev1.EventID,      // Event 1
            CategoryId = cat2.CategoryID // but category belongs to Event 2
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // TEST 9: Event not found → 404
    // ============================================================
    [Fact]
    public async Task Create_EventNotFound_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_EventNotFound_ReturnsNotFound));
        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentCreateDto
        {
            EventId = 9999,
            CategoryId = 1
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // TEST 10: Participant cancels their own enrolment → 200 OK
    // ============================================================
    [Fact]
    public async Task UpdateStatus_OwnEnrollment_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(UpdateStatus_OwnEnrollment_ReturnsOk));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);
        var en = await InsertEnrollmentAsync(db, userId: 10, eventId: ev.EventID, categoryId: cat.CategoryID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentStatusUpdateDto { Status = "Cancelled" };

        // ACT
        var result = await controller.UpdateStatus(en.EnrollmentID, dto);

        // ASSERT
        Assert.IsType<OkObjectResult>(result);

        var updated = await db.Enrollments.FindAsync(en.EnrollmentID);
        Assert.NotNull(updated);
        Assert.Equal("Cancelled", updated!.Status);
    }

    // ============================================================
    // TEST 11: Participant can't cancel someone else's enrolment → 403
    // ============================================================
    [Fact]
    public async Task UpdateStatus_OtherUsersEnrollment_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(UpdateStatus_OtherUsersEnrollment_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);
        // Enrollment belongs to user 20
        var en = await InsertEnrollmentAsync(db, userId: 20, eventId: ev.EventID, categoryId: cat.CategoryID);

        // But we act as user 10
        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentStatusUpdateDto { Status = "Cancelled" };

        // ACT
        var result = await controller.UpdateStatus(en.EnrollmentID, dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 12: Invalid status value → 400 Bad Request
    // ============================================================
    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsBadRequest()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(UpdateStatus_InvalidStatus_ReturnsBadRequest));
        var ev = await InsertEventAsync(db, organiserId: 1);
        var cat = await InsertCategoryAsync(db, ev.EventID);
        var en = await InsertEnrollmentAsync(db, userId: 10, eventId: ev.EventID, categoryId: cat.CategoryID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentStatusUpdateDto { Status = "InvalidStatus" };

        // ACT
        var result = await controller.UpdateStatus(en.EnrollmentID, dto);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ============================================================
    // TEST 13: Enrolment not found → 404
    // ============================================================
    [Fact]
    public async Task UpdateStatus_NonExistentEnrollment_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(UpdateStatus_NonExistentEnrollment_ReturnsNotFound));
        var controller = BuildController(db, userId: 10, role: "Participant");

        var dto = new EnrollmentStatusUpdateDto { Status = "Cancelled" };

        // ACT
        var result = await controller.UpdateStatus(9999, dto);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }
}