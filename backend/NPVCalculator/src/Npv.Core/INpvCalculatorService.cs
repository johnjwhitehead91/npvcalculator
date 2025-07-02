using Npv.Contracts.Models;
using Npv.Contracts.Requests;
using Npv.Contracts.Responses;

namespace Npv.Core;

public interface INpvCalculatorService
{
    Task<string> StartCalculationAsync(NpvRequest request);
    Task<NpvCalculationResponse?> GetResultsAsync(string calculationId);
    Task<NpvCalculationStatus> GetStatusAsync(string calculationId);
    Task<List<NpvResult>> CalculateNpvAsync(NpvRequest request);
    Task<List<NpvCalculationHistoryItem>> GetCalculationHistoryAsync();
}