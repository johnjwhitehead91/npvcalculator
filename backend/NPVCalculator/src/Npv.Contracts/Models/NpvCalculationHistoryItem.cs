namespace Npv.Contracts.Models;

public class NpvCalculationHistoryItem
{
    public string CalculationId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal LowerBoundDiscountRate { get; set; }
    public decimal UpperBoundDiscountRate { get; set; }
    public decimal DiscountRateIncrement { get; set; }
    public decimal InitialInvestment { get; set; }
    public int CashFlowCount { get; set; }
}