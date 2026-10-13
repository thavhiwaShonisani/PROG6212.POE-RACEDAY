using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Models;
using RaceDayManagementAPI.Tests.TestHelpers;
using Xunit;

namespace RaceDayManagementAPI.Tests.Controllers;

public class CategoriesControllerTests
{
    // -------------------- GET /api/categories --------------------

    [Fact]
    public async Task GetCategories_NoFilter_ReturnsAllSixSeededCategories()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 3, role: "Participant");

        var result = await controller.GetCategories(null);

        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value);
        Assert.Equal(6, categories.Count());
    }

    [Fact]
    public async Task GetCategories_WithEventId_FiltersToThatEvent()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 3, role: "Participant");

        var result = await controller.GetCategories(eventId: 1);

        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(result.Value).ToList();
        Assert.Equal(2, categories.Count);
        Assert.All(categories, c => Assert.Equal(1, c.EventID));
    }

    // -------------------- GET /api/categories/{id} --------------------

    [Fact]
    public async Task GetCategory_ExistingId_ReturnsCategory()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 3, role: "Participant");

        var result = await controller.GetCategory(1);

        var category = Assert.IsType<Category>(result.Value);
        Assert.Equal("5km Fun Run", category.CategoryName);
    }

    [Fact]
    public async Task GetCategory_UnknownId_ReturnsNotFound()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 3, role: "Participant");

        var result = await controller.GetCategory(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // -------------------- POST /api/categories --------------------

    [Fact]
    public async Task CreateCategory_AsOwningOrganiser_ReturnsCreated()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Thabo (id 1) organises event 1 — valid target
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var newCategory = new Category
        {
            EventID = 1,
            CategoryName = "15km Trail",
            DistanceKm = 15.0m,
            EntryFee = 150.00m
        };

        var result = await controller.CreateCategory(newCategory);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var category = Assert.IsType<Category>(created.Value);
        Assert.True(category.CategoryID > 0);

        // Verify it actually landed in the store
        var stored = await db.Categories.FindAsync(category.CategoryID);
        Assert.NotNull(stored);
        Assert.Equal("15km Trail", stored!.CategoryName);
    }

    [Fact]
    public async Task CreateCategory_AsNonOwningOrganiser_Returns403()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Lindiwe (id 2) does NOT own event 1 (Thabo does)
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 2, role: "Organiser");

        var newCategory = new Category
        {
            EventID = 1,
            CategoryName = "Sneaky Addition",
            DistanceKm = 5.0m,
            EntryFee = 0m
        };

        var result = await controller.CreateCategory(newCategory);

        var status = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_NonExistentEvent_ReturnsBadRequest()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var newCategory = new Category
        {
            EventID = 999,
            CategoryName = "Orphan",
            DistanceKm = 5.0m,
            EntryFee = 0m
        };

        var result = await controller.CreateCategory(newCategory);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCategory_NegativeEntryFee_ReturnsBadRequest()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var newCategory = new Category
        {
            EventID = 1,
            CategoryName = "Discount",
            DistanceKm = 5.0m,
            EntryFee = -10m
        };

        var result = await controller.CreateCategory(newCategory);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // -------------------- PUT /api/categories/{id} --------------------

    [Fact]
    public async Task UpdateCategory_AsOwningOrganiser_UpdatesFields()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var updated = new Category
        {
            CategoryID = 1,
            EventID = 1,
            CategoryName = "5km Fun Walk",
            DistanceKm = 5.0m,
            EntryFee = 90.00m
        };

        var result = await controller.UpdateCategory(1, updated);

        Assert.IsType<NoContentResult>(result);

        var stored = await db.Categories.FindAsync(1);
        Assert.Equal("5km Fun Walk", stored!.CategoryName);
        Assert.Equal(90.00m, stored.EntryFee);
    }

    [Fact]
    public async Task UpdateCategory_IdMismatch_ReturnsBadRequest()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var updated = new Category { CategoryID = 2, EventID = 1, CategoryName = "X" };

        var result = await controller.UpdateCategory(1, updated);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCategory_AsNonOwningOrganiser_Returns403()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Category 1 belongs to event 1 (Thabo). Lindiwe tries to edit it.
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 2, role: "Organiser");

        var updated = new Category
        {
            CategoryID = 1,
            EventID = 1,
            CategoryName = "Hijacked",
            DistanceKm = 5.0m,
            EntryFee = 0m
        };

        var result = await controller.UpdateCategory(1, updated);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_UnknownId_ReturnsNotFound()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var updated = new Category { CategoryID = 999, EventID = 1, CategoryName = "X" };

        var result = await controller.UpdateCategory(999, updated);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // -------------------- DELETE /api/categories/{id} --------------------

    [Fact]
    public async Task DeleteCategory_AsOwningOrganiser_RemovesCategory()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Category 1 has no enrolments → safe to delete
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var result = await controller.DeleteCategory(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.Categories.FindAsync(1));
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_ReturnsConflict()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Category 2 has enrolment 1 (Sarah)
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var result = await controller.DeleteCategory(2);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflict.Value);
        Assert.NotNull(await db.Categories.FindAsync(2));   // still there
    }

    [Fact]
    public async Task DeleteCategory_AsNonOwningOrganiser_Returns403()
    {
        using var db = ControllerFactory.CreateDbContext();
        // Category 5 belongs to event 3 (Lindiwe). Thabo tries to delete it.
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var result = await controller.DeleteCategory(5);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
        Assert.NotNull(await db.Categories.FindAsync(5));
    }

    [Fact]
    public async Task DeleteCategory_UnknownId_ReturnsNotFound()
    {
        using var db = ControllerFactory.CreateDbContext();
        var controller = ControllerFactory.CreateCategoriesController(db, userId: 1, role: "Organiser");

        var result = await controller.DeleteCategory(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}