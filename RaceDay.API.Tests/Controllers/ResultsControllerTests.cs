using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Results;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Controllers;

public class ResultsControllerTests
{
    // ============================================================
    // Helper: build a controller with a fake logged-in user
    // ============================================================
    private static ResultsController BuildController(RaceDayDbContext db, int? userId, string? role)
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

        var controller = new ResultsController(db, currentUser)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return controller;
    }

    /// <summary>Insert an event, its category, and an enrollment in one shot.</summary>
    private static async Task<(Event ev, Category cat, Enrollment en)> SeedEventAndEnrollmentAsync(
        RaceDayDbContext db, int organiserId, int participantId)
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

        var cat = new Category { EventID = ev.EventID, CategoryName = "Open" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var en = new Enrollment
        {
            UserID = participantId,
            EventID = ev.EventID,
            CategoryID = cat.CategoryID,
            EnrollmentDate = DateTime.Now,
            Status = "Confirmed"
        };
        db.Enrollments.Add(en);
        await db.SaveChangesAsync();

        return (ev, cat, en);
    }

    /// <summary>Insert a result for the given enrollment.</summary>
    private static async Task<Result> InsertResultAsync(
        RaceDayDbContext db, int enrollmentId, int minutes = 42, int position = 3)
    {
        var r = new Result
        {
            EnrollmentID = enrollmentId,
            FinishTime = TimeSpan.FromMinutes(minutes),
            Position = position
        };
        db.Results.Add(r);
        await db.SaveChangesAsync();
        return r;
    }

    // ============================================================
    // TEST 1: Participant sees only their own results
    // ============================================================
    [Fact]
    public async Task MyResults_ReturnsOnlyOwnResults()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(MyResults_ReturnsOnlyOwnResults));

        var (_, _, en10) = await SeedEventAndEnrollmentAsync(db, organiserId: 1, participantId: 10);
        await InsertResultAsync(db, en10.EnrollmentID, minutes: 42, position: 1);

        var (_, _, en20) = await SeedEventAndEnrollmentAsync(db, organiserId: 1, participantId: 20);
        await InsertResultAsync(db, en20.EnrollmentID, minutes: 50, position: 2);

        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.MyResults();

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value)
            .Cast<ResultDto>().ToList();
        Assert.Single(list);
        Assert.Equal(en10.EnrollmentID, list[0].EnrollmentID);
    }

    // ============================================================
    // TEST 2: Participant with no results gets empty list
    // ============================================================
    [Fact]
    public async Task MyResults_NoResults_ReturnsEmptyList()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(MyResults_NoResults_ReturnsEmptyList));
        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.MyResults();

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value)
            .Cast<ResultDto>().ToList();
        Assert.Empty(list);
    }

    // ============================================================
    // TEST 3: Participant views own result → 200 OK
    // ============================================================
    [Fact]
    public async Task GetForEnrollment_OwnEnrollment_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEnrollment_OwnEnrollment_ReturnsOk));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 1, participantId: 10);
        await InsertResultAsync(db, en.EnrollmentID);

        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.GetForEnrollment(en.EnrollmentID);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ResultDto>(ok.Value);
        Assert.Equal(en.EnrollmentID, dto.EnrollmentID);
    }

    // ============================================================
    // TEST 4: Organiser views result for own event's enrolment → 200 OK
    // ============================================================
    [Fact]
    public async Task GetForEnrollment_OrganiserOwner_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEnrollment_OrganiserOwner_ReturnsOk));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 42, participantId: 10);
        await InsertResultAsync(db, en.EnrollmentID);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.GetForEnrollment(en.EnrollmentID);

        // ASSERT
        Assert.IsType<OkObjectResult>(result);
    }

    // ============================================================
    // TEST 5: Other participant can't view someone else's result → 403
    // ============================================================
    [Fact]
    public async Task GetForEnrollment_OtherParticipant_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEnrollment_OtherParticipant_ReturnsForbidden));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 1, participantId: 20);
        await InsertResultAsync(db, en.EnrollmentID);

        // Act as participant 10
        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.GetForEnrollment(en.EnrollmentID);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 6: No result captured yet → 404
    // ============================================================
    [Fact]
    public async Task GetForEnrollment_NoResultCaptured_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEnrollment_NoResultCaptured_ReturnsNotFound));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 1, participantId: 10);

        var controller = BuildController(db, userId: 10, role: "Participant");

        // ACT
        var result = await controller.GetForEnrollment(en.EnrollmentID);

        // ASSERT
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // TEST 7: Organiser captures a new result → 201 Created
    // ============================================================
    [Fact]
    public async Task Create_AsOwnerOrganiser_ReturnsCreated()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_AsOwnerOrganiser_ReturnsCreated));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 42, participantId: 10);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultCreateDto
        {
            EnrollmentId = en.EnrollmentID,
            FinishTime = TimeSpan.FromMinutes(30),
            Position = 1
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<CreatedAtActionResult>(result);

        var saved = await db.Results.FirstOrDefaultAsync(r => r.EnrollmentID == en.EnrollmentID);
        Assert.NotNull(saved);
        Assert.Equal(TimeSpan.FromMinutes(30), saved!.FinishTime);
        Assert.Equal(1, saved.Position);
    }

    // ============================================================
    // TEST 8: Duplicate result → 409 Conflict
    // ============================================================
    [Fact]
    public async Task Create_DuplicateResult_ReturnsConflict()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_DuplicateResult_ReturnsConflict));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 42, participantId: 10);
        await InsertResultAsync(db, en.EnrollmentID);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultCreateDto
        {
            EnrollmentId = en.EnrollmentID,
            FinishTime = TimeSpan.FromMinutes(35),
            Position = 2
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<ConflictObjectResult>(result);
    }

    // ============================================================
    // TEST 9: Organiser tries to capture result for another
    //         organiser's event → 403 Forbidden
    // ============================================================
    [Fact]
    public async Task Create_OtherOrganisersEnrollment_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_OtherOrganisersEnrollment_ReturnsForbidden));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 999, participantId: 10);

        // Act as organiser 42
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultCreateDto
        {
            EnrollmentId = en.EnrollmentID,
            FinishTime = TimeSpan.FromMinutes(30),
            Position = 1
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 10: Enrollment not found → 404
    // ============================================================
    [Fact]
    public async Task Create_EnrollmentNotFound_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_EnrollmentNotFound_ReturnsNotFound));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultCreateDto
        {
            EnrollmentId = 9999,
            FinishTime = TimeSpan.FromMinutes(30),
            Position = 1
        };

        // ACT
        var result = await controller.Create(dto);

        // ASSERT
        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // TEST 11: Organiser updates their own result → 200 OK
    // ============================================================
    [Fact]
    public async Task Update_OwnResult_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OwnResult_ReturnsOk));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 42, participantId: 10);
        var existing = await InsertResultAsync(db, en.EnrollmentID, minutes: 42, position: 3);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultUpdateDto
        {
            FinishTime = TimeSpan.FromMinutes(40),
            Position = 2
        };

        // ACT
        var result = await controller.Update(existing.ResultID, dto);

        // ASSERT
        Assert.IsType<OkObjectResult>(result);

        var updated = await db.Results.FindAsync(existing.ResultID);
        Assert.NotNull(updated);
        Assert.Equal(TimeSpan.FromMinutes(40), updated!.FinishTime);
        Assert.Equal(2, updated.Position);
    }

    // ============================================================
    // TEST 12: Organiser updates another organiser's result → 403
    // ============================================================
    [Fact]
    public async Task Update_OtherOrganisersResult_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OtherOrganisersResult_ReturnsForbidden));
        var (_, _, en) = await SeedEventAndEnrollmentAsync(db, organiserId: 999, participantId: 10);
        var existing = await InsertResultAsync(db, en.EnrollmentID);

        // Act as organiser 42
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultUpdateDto
        {
            FinishTime = TimeSpan.FromMinutes(20),
            Position = 1
        };

        // ACT
        var result = await controller.Update(existing.ResultID, dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 13: Result not found → 404
    // ============================================================
    [Fact]
    public async Task Update_NonExistentResult_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_NonExistentResult_ReturnsNotFound));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new ResultUpdateDto
        {
            FinishTime = TimeSpan.FromMinutes(20),
            Position = 1
        };

        // ACT
        var result = await controller.Update(9999, dto);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }
}