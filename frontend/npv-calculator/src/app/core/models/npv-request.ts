export interface NpvRequest {
  initialInvestment: number;
  cashFlows: number[];
  lowerBoundDiscountRate: number;
  upperBoundDiscountRate: number;
  discountRateIncrement: number;
  calculationId?: string;
}
