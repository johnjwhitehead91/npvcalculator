using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Npv.Contracts.Models;
using Npv.Contracts.Requests;
using Npv.Contracts.Responses;
using Npv.Core;

namespace Npv.Services;

public class NpvCalculatorService : INpvCalculatorService
{
    private readonly ILogger<NpvCalculatorService> _logger;
    private readonly ConcurrentDictionary<string, NpvCalculationStatus> _calculationStatuses;
    private readonly ConcurrentDictionary<string, NpvCalculationResponse> _calculationResults;
    private readonly Timer _cleanupTimer;

    public NpvCalculatorService(ILogger<NpvCalculatorService> logger)
    {
        _logger = logger;
        _calculationStatuses = new ConcurrentDictionary<string, NpvCalculationStatus>();
        _calculationResults = new ConcurrentDictionary<string, NpvCalculationResponse>();

        // Cleanup old calculations every 30 minutes
        _cleanupTimer = new Timer(CleanupOldCalculations, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    public async Task<string> StartCalculationAsync(NpvRequest request)
    {
        var calculationId = Guid.NewGuid().ToString();

        var status = new NpvCalculationStatus
        {
            CalculationId = calculationId,
            Status = "Processing",
            StartedAt = DateTime.UtcNow,
            Progress = 0,
            TotalCalculations = CalculateTotalCalculations(request.LowerBoundDiscountRate,
                request.UpperBoundDiscountRate, request.DiscountRateIncrement)
        };

        _calculationStatuses.TryAdd(calculationId, status);

        // Start calculation in background
        _ = Task.Run(async () =>
        {
            try
            {
                var results = await CalculateNpvAsync(request, calculationId);
                var response = new NpvCalculationResponse
                {
                    CalculationId = calculationId,
                    Results = results,
                    Status = "Completed",
                    CalculatedAt = DateTime.UtcNow,
                    Request = request
                };

                _calculationResults.TryAdd(calculationId, response);

                // Thread-safe status update
                _calculationStatuses.AddOrUpdate(calculationId, status, (key, existing) =>
                {
                    existing.Status = "Completed";
                    existing.Progress = existing.TotalCalculations;
                    existing.CompletedAt = DateTime.UtcNow;
                    return existing;
                });

                _logger.LogInformation("NPV calculation completed for ID: {CalculationId}", calculationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating NPV for ID: {CalculationId}", calculationId);

                // Thread-safe error status update
                _calculationStatuses.AddOrUpdate(calculationId, status, (key, existing) =>
                {
                    existing.Status = "Failed";
                    existing.CompletedAt = DateTime.UtcNow;
                    return existing;
                });
            }
        });

        return calculationId;
    }

    public async Task<NpvCalculationResponse?> GetResultsAsync(string calculationId)
    {
        if (_calculationResults.TryGetValue(calculationId, out var result))
        {
            return await Task.FromResult(result);
        }

        return null;
    }

    public async Task<NpvCalculationStatus> GetStatusAsync(string calculationId)
    {
        if (_calculationStatuses.TryGetValue(calculationId, out var status))
        {
            return await Task.FromResult(status);
        }

        return new NpvCalculationStatus
        {
            CalculationId = calculationId,
            Status = "NotFound"
        };
    }

    public Task<List<NpvResult>> CalculateNpvAsync(NpvRequest request)
    {
        return CalculateNpvAsync(request, null);
    }

    private async Task<List<NpvResult>> CalculateNpvAsync(NpvRequest request, string? calculationId)
    {
        var results = new List<NpvResult>();
        var discountRates = GenerateDiscountRates(request.LowerBoundDiscountRate, request.UpperBoundDiscountRate,
            request.DiscountRateIncrement);

        for (int i = 0; i < discountRates.Count; i++)
        {
            var discountRate = discountRates[i];
            // UPDATED: Pass initial investment to NPV calculation
            var npv = CalculateNpv(request.InitialInvestment, request.CashFlows, discountRate);

            results.Add(new NpvResult
            {
                DiscountRate = discountRate,
                NPV = npv,
                Period = i + 1
            });

            // Update progress if calculation ID is provided
            if (!string.IsNullOrEmpty(calculationId))
            {
                _calculationStatuses.AddOrUpdate(calculationId,
                    new NpvCalculationStatus(),
                    (key, existing) =>
                    {
                        existing.Progress = i + 1;
                        return existing;
                    });
            }

            // Simulate some processing time for demonstration
            await Task.Delay(10);
        }

        return results;
    }

    private int CalculateTotalCalculations(decimal lowerBound, decimal upperBound, decimal increment)
    {
        if (increment <= 0) return 0;
        return (int)Math.Floor((upperBound - lowerBound) / increment) + 1;
    }

    private List<decimal> GenerateDiscountRates(decimal lowerBound, decimal upperBound, decimal increment)
    {
        var rates = new List<decimal>();
        var currentRate = lowerBound;

        while (currentRate <= upperBound)
        {
            rates.Add(Math.Round(currentRate, 2)); // Round to avoid floating point precision issues
            currentRate += increment;
        }

        return rates;
    }

    private decimal CalculateNpv(decimal initialInvestment, List<decimal>? cashFlows, decimal discountRate)
    {
        // Start with initial investment (Period 0 - no discounting)
        decimal npv = initialInvestment;
        
        // If no future cash flows, NPV is just the initial investment
        if (cashFlows == null || cashFlows.Count == 0)
            return Math.Round(npv, 2);

        var rate = discountRate / 100; // Convert percentage to decimal

        // Add discounted future cash flows (Period 1, 2, 3, etc.)
        for (int i = 0; i < cashFlows.Count; i++)
        {
            var cashFlow = cashFlows[i];
            var timePeriod = i + 1; // Period 1, 2, 3, etc.
            var discountFactor = (decimal)Math.Pow((double)(1 + rate), timePeriod);
            npv += cashFlow / discountFactor;
        }

        return Math.Round(npv, 2);
    }

    public async Task<List<NpvCalculationHistoryItem>> GetCalculationHistoryAsync()
    {
        var history = new List<NpvCalculationHistoryItem>();

        foreach (var kvp in _calculationStatuses)
        {
            var status = kvp.Value;
            NpvRequest? request = null;

            if (_calculationResults.TryGetValue(kvp.Key, out var result))
            {
                request = result.Request;
            }

            var item = new NpvCalculationHistoryItem
            {
                CalculationId = status.CalculationId,
                StartedAt = status.StartedAt,
                CompletedAt = status.CompletedAt,
                Status = status.Status,
                InitialInvestment = request?.InitialInvestment ?? 0, // UPDATED: Include initial investment
                LowerBoundDiscountRate = request?.LowerBoundDiscountRate ?? 0,
                UpperBoundDiscountRate = request?.UpperBoundDiscountRate ?? 0,
                DiscountRateIncrement = request?.DiscountRateIncrement ?? 0,
                CashFlowCount = request?.CashFlows?.Count ?? 0
            };
            history.Add(item);
        }

        return await Task.FromResult(history.OrderByDescending(h => h.StartedAt).ToList());
    }

    private void CleanupOldCalculations(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24); // Remove calculations older than 24 hours
            var keysToRemove = new List<string>();

            foreach (var kvp in _calculationStatuses)
            {
                if (kvp.Value.StartedAt < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _calculationStatuses.TryRemove(key, out _);
                _calculationResults.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old NPV calculations", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during NPV calculation cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}