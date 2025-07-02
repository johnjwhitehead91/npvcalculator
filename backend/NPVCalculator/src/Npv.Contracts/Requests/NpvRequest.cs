using System.ComponentModel.DataAnnotations;

namespace Npv.Contracts.Requests;

public class NpvRequest
{
    public decimal InitialInvestment { get; set; }  // Period 0

    [Required]
    [MinLength(1, ErrorMessage = "At least one cash flow is required")]
    public List<decimal>? CashFlows { get; set; } = new();

    [Required]
    [Range(0.01, 100, ErrorMessage = "Lower bound discount rate must be between 0.01% and 100%")]
    public decimal LowerBoundDiscountRate { get; set; }

    [Required]
    [Range(0.01, 100, ErrorMessage = "Upper bound discount rate must be between 0.01% and 100%")]
    public decimal UpperBoundDiscountRate { get; set; }

    [Required]
    [Range(0.01, 10, ErrorMessage = "Discount rate increment must be between 0.01% and 10%")]
    public decimal DiscountRateIncrement { get; set; }

    public string? CalculationId { get; set; }
}