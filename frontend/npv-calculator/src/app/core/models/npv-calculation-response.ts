import {NpvResult} from './npv-result';
import {NpvRequest} from './npv-request';

export interface NpvCalculationResponse {
  calculationId: string;
  results: NpvResult[];
  status: string;
  calculatedAt: string;
  request: NpvRequest;
}
