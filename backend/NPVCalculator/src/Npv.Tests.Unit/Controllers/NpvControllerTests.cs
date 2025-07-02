using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Npv.Contracts.Models;
using Npv.Contracts.Requests;
using Npv.Contracts.Responses;
using Npv.Core;
using Npv.WebApi.Controllers;
using Xunit;

namespace Npv.Tests.Unit.Controllers;

public class NpvControllerTests
{
    private readonly Mock<INpvCalculatorService> _mockService;
    private readonly Mock<ILogger<NpvController>> _mockLogger;
    private readonly NpvController _controller;

    public NpvControllerTests()
    {
        _mockService = new Mock<INpvCalculatorService>();
        _mockLogger = new Mock<ILogger<NpvController>>();
        _controller = new NpvController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateNPV_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new NpvRequest
        {
            CashFlows = new List<decimal> { -1000, 300, 400, 500 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        var calculationId = Guid.NewGuid().ToString();
        _mockService.Setup(x => x.StartCalculationAsync(request))
            .ReturnsAsync(calculationId);

        // Act
        var result = await _controller.CalculateNpv(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var calculationIdValue = okResult.Value?.GetType().GetProperty("CalculationId")?.GetValue(okResult.Value, null);
        Assert.Equal(calculationId, calculationIdValue);
    }

    [Fact]
    public async Task CalculateNPV_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new NpvRequest
        {
            CashFlows = new List<decimal>(), // Invalid - empty cash flows
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        _controller.ModelState.AddModelError("CashFlows", "At least one cash flow is required");

        // Act
        var result = await _controller.CalculateNpv(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CalculateNPV_WithUpperBoundLessThanLowerBound_ReturnsBadRequest()
    {
        // Arrange
        var request = new NpvRequest
        {
            CashFlows = new List<decimal> { -1000, 300, 400 },
            LowerBoundDiscountRate = 12,
            UpperBoundDiscountRate = 10, // Invalid - less than lower bound
            DiscountRateIncrement = 1
        };

        // Act
        var result = await _controller.CalculateNpv(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Upper bound discount rate must be greater than lower bound discount rate",
            badRequestResult.Value);
    }

    [Fact]
    public async Task GetResults_WithValidCalculationId_ReturnsOkResult()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        var expectedResponse = new NpvCalculationResponse
        {
            CalculationId = calculationId,
            Results = new List<NpvResult>
            {
                new NpvResult { DiscountRate = 10, NPV = 100, Period = 1 },
                new NpvResult { DiscountRate = 11, NPV = 95, Period = 2 }
            },
            Status = "Completed"
        };

        _mockService.Setup(x => x.GetResultsAsync(calculationId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetResults(calculationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NpvCalculationResponse>(okResult.Value);
        Assert.Equal(calculationId, response.CalculationId);
        Assert.Equal(2, response.Results.Count);
    }

    [Fact]
    public async Task GetResults_WithInvalidCalculationId_ReturnsNotFound()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        _mockService.Setup(x => x.GetResultsAsync(calculationId))
            .ReturnsAsync((NpvCalculationResponse?)null);

        // Act
        var result = await _controller.GetResults(calculationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"No results found for calculation ID: {calculationId}", notFoundResult.Value);
    }

    [Fact]
    public async Task GetStatus_WithValidCalculationId_ReturnsOkResult()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        var expectedStatus = new NpvCalculationStatus
        {
            CalculationId = calculationId,
            Status = "Processing",
            Progress = 50,
            TotalCalculations = 100
        };

        _mockService.Setup(x => x.GetStatusAsync(calculationId))
            .ReturnsAsync(expectedStatus);

        // Act
        var result = await _controller.GetStatus(calculationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var status = Assert.IsType<NpvCalculationStatus>(okResult.Value);
        Assert.Equal(calculationId, status.CalculationId);
        Assert.Equal("Processing", status.Status);
    }

    [Fact]
    public async Task GetStatus_WithInvalidCalculationId_ReturnsNotFound()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        var notFoundStatus = new NpvCalculationStatus
        {
            CalculationId = calculationId,
            Status = "NotFound"
        };

        _mockService.Setup(x => x.GetStatusAsync(calculationId))
            .ReturnsAsync(notFoundStatus);

        // Act
        var result = await _controller.GetStatus(calculationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"No calculation found with ID: {calculationId}", notFoundResult.Value);
    }

    [Fact]
    public async Task CalculateNPV_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new NpvRequest
        {
            CashFlows = new List<decimal> { -1000, 300, 400 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        _mockService.Setup(x => x.StartCalculationAsync(request))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.CalculateNpv(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while starting the calculation", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetResults_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        _mockService.Setup(x => x.GetResultsAsync(calculationId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetResults(calculationId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving results", statusCodeResult.Value);
    }

    [Fact]
    public async Task GetStatus_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var calculationId = Guid.NewGuid().ToString();
        _mockService.Setup(x => x.GetStatusAsync(calculationId))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStatus(calculationId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving status", statusCodeResult.Value);
    }
}

// Helper class for anonymous objects
public class Anonymous
{
    public string CalculationId { get; set; } = string.Empty;
}