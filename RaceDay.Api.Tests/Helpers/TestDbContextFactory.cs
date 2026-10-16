using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static RaceDayDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(
                    databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new RaceDayDbContext(options);

            context.Database.EnsureCreated();

            return context;
        }
    }
}