using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Helpers;
using Xunit;

namespace RaceDay.Api.Tests.Controllers
{
    public class CategoriesControllerTests
    {
        [Fact]
        public async Task GetCategoriesByEvent_ReturnsOk_WhenEventExists()
        {
            using var context = TestDbContextFactory.Create();

            context.Events.Add(new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Category Test Event",
                EventDate = new DateOnly(2026, 12, 5),
                Location = "Pretoria",
                DistanceKm = 10m,
                EventType = "Run",
                CreatedAt = DateTime.UtcNow
            });

            context.Categories.Add(new Category
            {
                CategoryId = 1,
                EventId = 1,
                CategoryName = "10km Open",
                MinAge = 18,
                MaxAge = 99,
                DistanceKm = 10m
            });

            await context.SaveChangesAsync();

            var controller = new CategoriesController(context);

            var result = await controller.GetCategoriesByEvent(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetCategoriesByEvent_Returns404_WhenEventDoesNotExist()
        {
            using var context = TestDbContextFactory.Create();

            var controller = new CategoriesController(context);

            var result = await controller.GetCategoriesByEvent(999999);

            var statusCode = result switch
            {
                ObjectResult objectResult => objectResult.StatusCode,
                StatusCodeResult statusResult => statusResult.StatusCode,
                _ => null
            };

            Assert.Equal(404, statusCode);
        }
    }
}