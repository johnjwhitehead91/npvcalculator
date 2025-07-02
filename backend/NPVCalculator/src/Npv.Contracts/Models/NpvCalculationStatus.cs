namespace Npv.Contracts.Models;

public class NpvCalculationStatus
{
    public string CalculationId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Progress { get; set; } = 0;
    public int TotalCalculations { get; set; } = 0;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}