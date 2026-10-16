using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.API.Tests.Helpers;

public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a fresh in-memory database for tests.
    /// Use a unique name per test (typically nameof(TheTest)).
    /// </summary>
    public static RaceDayDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new RaceDayDbContext(options);

        // Seed the in-memory DB with the data defined via HasData in the DbContext
        context.Database.EnsureCreated();

        return context;
    }
}
