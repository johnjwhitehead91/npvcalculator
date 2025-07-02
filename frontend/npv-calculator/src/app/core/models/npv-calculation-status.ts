export interface NpvCalculationStatus {
  calculationId: string;
  status: string;
  progress: number;
  totalCalculations: number;
  startedAt: string;
  completedAt?: string;
}
