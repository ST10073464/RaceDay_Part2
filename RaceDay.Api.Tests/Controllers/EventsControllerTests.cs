using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Helpers;
using Xunit;

namespace RaceDay.Api.Tests.Controllers
{
    public class EventsControllerTests
    {
        private static BlobStorageService CreateBlobStorageService()
        {
            var settings =
                new Dictionary<string, string?>
                {
                    ["AzureStorage:ConnectionString"] =
                        "DefaultEndpointsProtocol=https;" +
                        "AccountName=testaccount;" +
                        "AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==;" +
                        "EndpointSuffix=core.windows.net",

                    ["AzureStorage:ProfilePicturesContainer"] =
                        "profile-pictures"
                };

            IConfiguration configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(settings)
                    .Build();

            return new BlobStorageService(configuration);
        }

        [Fact]
        public async Task GetEvents_ReturnsOk_WhenEventsExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            context.Events.Add(new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Pretoria Test Run",
                Description = "Unit test event",
                EventDate = new DateOnly(2026, 12, 5),
                Location = "Pretoria",
                DistanceKm = 10.00m,
                EventType = "Run",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var blobStorageService =
                CreateBlobStorageService();

            var controller =
                new EventsController(
                    context,
                    blobStorageService);

            // Act
            var result = await controller.GetEvents();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetEventById_Returns404_WhenEventDoesNotExist()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var blobStorageService =
                CreateBlobStorageService();

            var controller =
                new EventsController(
                    context,
                    blobStorageService);

            // Act
            var result = await controller.GetEventById(999999);

            // Assert
            var statusCode = result switch
            {
                ObjectResult objectResult => objectResult.StatusCode,
                StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
                _ => null
            };

            Assert.Equal(404, statusCode);
        }
    }
}