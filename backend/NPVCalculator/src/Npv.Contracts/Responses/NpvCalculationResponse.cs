using Npv.Contracts.Models;
using Npv.Contracts.Requests;

namespace Npv.Contracts.Responses;

public class NpvCalculationResponse
{
    public string CalculationId { get; set; } = string.Empty;
    public List<NpvResult> Results { get; set; } = new();
    public string Status { get; set; } = "Completed";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public NpvRequest Request { get; set; } = new NpvRequest();
}