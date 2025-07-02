namespace Npv.Contracts.Models;

public class NpvResult
{
    public decimal DiscountRate { get; set; }
    public decimal NPV { get; set; }
    public int Period { get; set; }
}