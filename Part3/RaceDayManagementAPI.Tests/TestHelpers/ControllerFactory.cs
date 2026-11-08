using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayManagementAPI.Controllers;
using RaceDayManagementAPI.Data;

namespace RaceDayManagementAPI.Tests.TestHelpers;

public static class ControllerFactory
{
    /// <summary>
    /// Creates a fresh in-memory database. The name is unique per call,
    /// so tests never share state even when run in parallel.
    /// The HasData seeds from RaceDayDbContext.OnModelCreating are applied.
    /// </summary>
    public static RaceDayDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>
    /// Builds a CategoriesController whose HttpContext.User carries
    /// the given user id and role — exactly what the JWT middleware
    /// would have produced at runtime.
    /// </summary>
    public static CategoriesController CreateCategoriesController(
        RaceDayDbContext db, int userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var controller = new CategoriesController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        return controller;
    }
}