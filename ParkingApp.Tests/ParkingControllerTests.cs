using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using ParkingApp.Controllers;
using ParkingApp.Data.Enum;
using ParkingApp.Data.Models;
using ParkingApp.Data;

public class ParkingControllerTests
{
    private readonly ParkingController _controller;
    private readonly ParkingAppDbContext _dbContext;
    private readonly Mock<IStringLocalizer<ParkingController>> _localizerMock;

    public ParkingControllerTests()
    {
        _localizerMock = new Mock<IStringLocalizer<ParkingController>>();

        var options = new DbContextOptionsBuilder<ParkingAppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _dbContext = new ParkingAppDbContext(options);

        _controller = new ParkingController(_dbContext, _localizerMock.Object);
    }

    [Fact]
    public void AddSubscriber_ValidData_AddsSubscriber()
    {
        // Arrange
        var parking = new Parking { Id = 1, Location = "Test Parking", Capacity = 50 };
        _dbContext.Parkings.Add(parking);
        _dbContext.SaveChanges();

        // Act
        var result = _controller.AddSubscriber(
            parkingId: 1,
            spot: "A1",
            name: "John Doe",
            engBusiness: "JD Corp",
            bgBusiness: "ДЖ Корп",
            email: "john@example.com",
            phoneNumbers: "123456789",
            barrierPhoneNumbers: "987654321",
            paymentMethod: PaymentMethod.Cash,
            priceInBgn: 100,
            paid: true
        );

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Single(_dbContext.Subscribers);
    }
}
