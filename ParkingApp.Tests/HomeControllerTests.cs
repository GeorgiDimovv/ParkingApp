using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ParkingApp.Controllers;
using ParkingApp.Data;
using ParkingApp.Data.Models;
using ParkingApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace ParkingApp.Tests
{
    public class HomeControllerTests
    {
        private readonly HomeController _controller;
        private readonly Mock<ILogger<HomeController>> _loggerMock;
        private readonly ParkingAppDbContext _dbContext;

        public HomeControllerTests()
        {
            _loggerMock = new Mock<ILogger<HomeController>>();

            var options = new DbContextOptionsBuilder<ParkingAppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _dbContext = new ParkingAppDbContext(options);

            _controller = new HomeController(_loggerMock.Object, _dbContext);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfParkingSummaries()
        {
            // Arrange
            _dbContext.Parkings.Add(new Parking { Id = 1, Location = "Test Location", Capacity = 100 });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ParkingSummaryViewModel>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }
    }

}
