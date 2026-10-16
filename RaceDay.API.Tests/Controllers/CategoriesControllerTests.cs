using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs.Categories;
using RaceDay.API.Models;
using RaceDay.API.Services;
using RaceDay.API.Tests.Helpers;
using System.Security.Claims;
using Xunit;

namespace RaceDay.API.Tests.Controllers;

public class CategoriesControllerTests
{
    // ============================================================
    // Helper: build a CategoriesController with a fake logged-in user
    // ============================================================
    private static CategoriesController BuildController(RaceDayDbContext db, int? userId, string? role)
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

        var controller = new CategoriesController(db, currentUser)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return controller;
    }

    /// <summary>
    /// Helper: insert an event + return it.
    /// </summary>
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

    /// <summary>
    /// Helper: insert a category under the given event.
    /// </summary>
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

    // ============================================================
    // TEST 1: GET categories for an event — happy path
    // ============================================================
    [Fact]
    public async Task GetForEvent_ValidEvent_ReturnsOkWithCategories()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEvent_ValidEvent_ReturnsOkWithCategories));
        var ev = await InsertEventAsync(db, organiserId: 42);
        await InsertCategoryAsync(db, ev.EventID, "Open");
        await InsertCategoryAsync(db, ev.EventID, "Senior");

        var controller = BuildController(db, userId: null, role: null);

        // ACT
        var result = await controller.GetForEvent(ev.EventID);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
        Assert.NotNull(list);
    }

    // ============================================================
    // TEST 2: GET categories for a non-existent event — 404
    // ============================================================
    [Fact]
    public async Task GetForEvent_InvalidEvent_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(GetForEvent_InvalidEvent_ReturnsNotFound));
        var controller = BuildController(db, userId: null, role: null);

        // ACT
        var result = await controller.GetForEvent(9999);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }

    // ============================================================
    // TEST 3: Create category — Organiser owns event → 201 Created
    // ============================================================
    [Fact]
    public async Task Create_AsOwnerOrganiser_ReturnsCreated()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_AsOwnerOrganiser_ReturnsCreated));
        var ev = await InsertEventAsync(db, organiserId: 42);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new CategoryUpsertDto
        {
            CategoryName = "Under 20",
            Description = "Young runners"
        };

        // ACT
        var result = await controller.Create(ev.EventID, dto);

        // ASSERT
        Assert.IsType<CreatedAtActionResult>(result);

        var saved = await db.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Under 20");
        Assert.NotNull(saved);
        Assert.Equal(ev.EventID, saved!.EventID);
    }

    // ============================================================
    // TEST 4: Create category — Organiser does NOT own event → 403
    // ============================================================
    [Fact]
    public async Task Create_OtherOrganisersEvent_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_OtherOrganisersEvent_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999);

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new CategoryUpsertDto { CategoryName = "Sneaky" };

        // ACT
        var result = await controller.Create(ev.EventID, dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 5: Create category — event not found → 404
    // ============================================================
    [Fact]
    public async Task Create_NonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Create_NonExistentEvent_ReturnsNotFound));
        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new CategoryUpsertDto { CategoryName = "Nope" };

        // ACT
        var result = await controller.Create(9999, dto);

        // ASSERT
        Assert.IsType<NotFoundResult>(result);
    }

    // ============================================================
    // TEST 6: Update category — owner → 200 Ok
    // ============================================================
    [Fact]
    public async Task Update_OwnCategory_ReturnsOk()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OwnCategory_ReturnsOk));
        var ev = await InsertEventAsync(db, organiserId: 42);
        var cat = await InsertCategoryAsync(db, ev.EventID, "Old Name");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new CategoryUpsertDto
        {
            CategoryName = "New Name",
            Description = "Updated description"
        };

        // ACT
        var result = await controller.Update(cat.CategoryID, dto);

        // ASSERT
        Assert.IsType<OkObjectResult>(result);

        var updated = await db.Categories.FindAsync(cat.CategoryID);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.CategoryName);
    }

    // ============================================================
    // TEST 7: Update category — different organiser → 403
    // ============================================================
    [Fact]
    public async Task Update_OtherOrganisersCategory_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Update_OtherOrganisersCategory_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999);
        var cat = await InsertCategoryAsync(db, ev.EventID, "Original");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        var dto = new CategoryUpsertDto { CategoryName = "Hacked" };

        // ACT
        var result = await controller.Update(cat.CategoryID, dto);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }

    // ============================================================
    // TEST 8: Delete category — no enrolments → 204 No Content
    // ============================================================
    [Fact]
    public async Task Delete_EmptyCategory_ReturnsNoContent()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_EmptyCategory_ReturnsNoContent));
        var ev = await InsertEventAsync(db, organiserId: 42);
        var cat = await InsertCategoryAsync(db, ev.EventID, "To Delete");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(cat.CategoryID);

        // ASSERT
        Assert.IsType<NoContentResult>(result);

        var stillThere = await db.Categories.FindAsync(cat.CategoryID);
        Assert.Null(stillThere);
    }

    // ============================================================
    // TEST 9: Delete category — has enrolments → 409 Conflict
    // ============================================================
    [Fact]
    public async Task Delete_CategoryWithEnrollments_ReturnsConflict()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_CategoryWithEnrollments_ReturnsConflict));
        var ev = await InsertEventAsync(db, organiserId: 42);
        var cat = await InsertCategoryAsync(db, ev.EventID, "In Use");

        db.Enrollments.Add(new Enrollment
        {
            UserID = 100,
            EventID = ev.EventID,
            CategoryID = cat.CategoryID,
            Status = "Confirmed"
        });
        await db.SaveChangesAsync();

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(cat.CategoryID);

        // ASSERT
        Assert.IsType<ConflictObjectResult>(result);
    }

    // ============================================================
    // TEST 10: Delete category — different organiser → 403
    // ============================================================
    [Fact]
    public async Task Delete_OtherOrganisersCategory_ReturnsForbidden()
    {
        // ARRANGE
        using var db = TestDbContextFactory.Create(nameof(Delete_OtherOrganisersCategory_ReturnsForbidden));
        var ev = await InsertEventAsync(db, organiserId: 999);
        var cat = await InsertCategoryAsync(db, ev.EventID, "Not Mine");

        var controller = BuildController(db, userId: 42, role: "Organiser");

        // ACT
        var result = await controller.Delete(cat.CategoryID);

        // ASSERT
        Assert.IsType<ForbidResult>(result);
    }
}