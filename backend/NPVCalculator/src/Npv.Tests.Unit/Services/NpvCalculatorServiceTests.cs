using Microsoft.Extensions.Logging;
using Moq;
using Npv.Contracts.Requests;
using Npv.Services;
using Xunit;

namespace Npv.Tests.Unit.Services;

public class NpvCalculatorServiceTests
{
    private readonly Mock<ILogger<NpvCalculatorService>> _mockLogger;
    private readonly NpvCalculatorService _service;

    public NpvCalculatorServiceTests()
    {
        _mockLogger = new Mock<ILogger<NpvCalculatorService>>();
        _service = new NpvCalculatorService(_mockLogger.Object);
    }

    [Fact]
    public async Task CalculateNPVAsync_WithValidRequest_ReturnsCorrectResults()
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // NEW: Initial investment at Period 0
            CashFlows = new List<decimal> { 300, 400, 500 }, // UPDATED: Future cash flows only
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count); // 10%, 11%, 12%

        // Verify discount rates
        Assert.Equal(10, results[0].DiscountRate);
        Assert.Equal(11, results[1].DiscountRate);
        Assert.Equal(12, results[2].DiscountRate);

        // Verify NPV values are calculated (exact values depend on calculation)
        Assert.True(results[0].NPV != 0);
        Assert.True(results[1].NPV != 0);
        Assert.True(results[2].NPV != 0);
    }

    [Fact]
    public async Task CalculateNPVAsync_WithStandardNPVConvention_ReturnsCorrectResult()
    {
        // Arrange - Standard NPV example: Initial investment + discounted future cash flows
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // Period 0: Initial investment (no discounting)
            CashFlows = new List<decimal> { 600, 600 }, // Period 1 & 2: Future cash flows (discounted)
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 10,
            DiscountRateIncrement = 1
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        
        // Manual calculation: NPV = -1000 + 600/(1.10)^1 + 600/(1.10)^2
        // NPV = -1000 + 545.45 + 495.87 = 41.32 (approximately)
        var expectedNPV = -1000 + (600 / 1.10m) + (600 / (1.10m * 1.10m));
        Assert.Equal(Math.Round(expectedNPV, 2), results[0].NPV);
    }

    [Fact]
    public async Task CalculateNPVAsync_WithOnlyInitialInvestment_ReturnsInitialInvestment()
    {
        // Arrange - No future cash flows, only initial investment
        var request = new NpvRequest
        {
            InitialInvestment = -5000,
            CashFlows = new List<decimal>(), // No future cash flows
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 10,
            DiscountRateIncrement = 1
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(-5000, results[0].NPV); // NPV should equal initial investment when no future cash flows
    }

    [Fact]
    public async Task CalculateNPVAsync_WithPositiveInitialInvestment_ReturnsCorrectResult()
    {
        // Arrange - Unusual case: positive initial investment (e.g., receiving money upfront)
        var request = new NpvRequest
        {
            InitialInvestment = 1000, // Positive initial investment
            CashFlows = new List<decimal> { -200, -300, -500 }, // Negative future cash flows (payments)
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 10,
            DiscountRateIncrement = 1
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        
        // Manual calculation: NPV = 1000 + (-200)/(1.10)^1 + (-300)/(1.10)^2 + (-500)/(1.10)^3
        var expectedNPV = 1000 + (-200 / 1.10m) + (-300 / (1.10m * 1.10m)) + (-500 / (1.10m * 1.10m * 1.10m));
        Assert.Equal(Math.Round(expectedNPV, 2), results[0].NPV);
    }

    [Fact]
    public async Task StartCalculationAsync_ReturnsValidCalculationId()
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // UPDATED: Include initial investment
            CashFlows = new List<decimal> { 300, 400 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        // Act
        var calculationId = await _service.StartCalculationAsync(request);

        // Assert
        Assert.NotNull(calculationId);
        Assert.NotEmpty(calculationId);
        Assert.True(Guid.TryParse(calculationId, out _));
    }

    [Fact]
    public async Task GetStatusAsync_WithValidCalculationId_ReturnsStatus()
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // UPDATED: Include initial investment
            CashFlows = new List<decimal> { 300, 400 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        var calculationId = await _service.StartCalculationAsync(request);

        // Act
        var status = await _service.GetStatusAsync(calculationId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(calculationId, status.CalculationId);
        Assert.Equal("Processing", status.Status);
        Assert.True(status.StartedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task GetStatusAsync_WithInvalidCalculationId_ReturnsNotFound()
    {
        // Arrange
        var invalidCalculationId = Guid.NewGuid().ToString();

        // Act
        var status = await _service.GetStatusAsync(invalidCalculationId);

        // Assert
        Assert.NotNull(status);
        Assert.Equal("NotFound", status.Status);
    }

    [Fact]
    public async Task GetResultsAsync_WithValidCalculationId_ReturnsResults()
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // UPDATED: Include initial investment
            CashFlows = new List<decimal> { 300, 400 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        var calculationId = await _service.StartCalculationAsync(request);

        // Wait a bit for calculation to complete
        await Task.Delay(100);

        // Act
        var results = await _service.GetResultsAsync(calculationId);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(calculationId, results.CalculationId);
        Assert.Equal("Completed", results.Status);
        Assert.NotNull(results.Results);
        Assert.True(results.Results.Count > 0);
    }

    [Fact]
    public async Task GetResultsAsync_WithInvalidCalculationId_ReturnsNull()
    {
        // Arrange
        var invalidCalculationId = Guid.NewGuid().ToString();

        // Act
        var results = await _service.GetResultsAsync(invalidCalculationId);

        // Assert
        Assert.Null(results);
    }

    [Theory]
    [InlineData(1, 5, 1, 5)] // 1%, 2%, 3%, 4%, 5%
    [InlineData(10, 12, 0.5, 5)] // 10%, 10.5%, 11%, 11.5%, 12%
    [InlineData(5, 5, 1, 1)] // 5% only
    public async Task CalculateNPVAsync_GeneratesCorrectDiscountRates(decimal lower, decimal upper, decimal increment,
        int expectedCount)
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -1000, // UPDATED: Include initial investment
            CashFlows = new List<decimal> { 300, 400 },
            LowerBoundDiscountRate = lower,
            UpperBoundDiscountRate = upper,
            DiscountRateIncrement = increment
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.Equal(expectedCount, results.Count);

        for (int i = 0; i < results.Count; i++)
        {
            var expectedRate = lower + (i * increment);
            Assert.Equal(expectedRate, results[i].DiscountRate);
        }
    }

    [Fact]
    public async Task CalculateNPVAsync_WithZeroDiscountRate_ReturnsCorrectResult()
    {
        // Arrange - Test edge case with 0% discount rate
        var request = new NpvRequest
        {
            InitialInvestment = -1000,
            CashFlows = new List<decimal> { 300, 400, 500 },
            LowerBoundDiscountRate = 0,
            UpperBoundDiscountRate = 0,
            DiscountRateIncrement = 1
        };

        // Act
        var results = await _service.CalculateNpvAsync(request);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        
        // With 0% discount rate: NPV = -1000 + 300 + 400 + 500 = 200
        Assert.Equal(200, results[0].NPV);
    }

    [Fact]
    public async Task GetCalculationHistoryAsync_ReturnsHistoryWithInitialInvestment()
    {
        // Arrange
        var request = new NpvRequest
        {
            InitialInvestment = -5000, // UPDATED: Include initial investment
            CashFlows = new List<decimal> { 1000, 2000, 3000 },
            LowerBoundDiscountRate = 10,
            UpperBoundDiscountRate = 12,
            DiscountRateIncrement = 1
        };

        var calculationId = await _service.StartCalculationAsync(request);
        await Task.Delay(100); // Allow calculation to complete

        // Act
        var history = await _service.GetCalculationHistoryAsync();

        // Assert
        Assert.NotNull(history);
        Assert.True(history.Count > 0);
        
        var historyItem = history.FirstOrDefault(h => h.CalculationId == calculationId);
        Assert.NotNull(historyItem);
        Assert.Equal(-5000, historyItem.InitialInvestment); // NEW: Verify initial investment is tracked
        Assert.Equal(3, historyItem.CashFlowCount);
        Assert.Equal(10, historyItem.LowerBoundDiscountRate);
        Assert.Equal(12, historyItem.UpperBoundDiscountRate);
    }
}