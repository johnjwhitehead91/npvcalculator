import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, timer} from 'rxjs';
import {switchMap, retryWhen, delayWhen} from 'rxjs/operators';
import {environment} from '../../environments/environment';
import {CalculationResponse} from '../core/models/calculation-response';
import {NpvRequest} from '../core/models/npv-request';
import {NpvCalculationResponse} from '../core/models/npv-calculation-response';
import {NpvCalculationStatus} from '../core/models/npv-calculation-status';

@Injectable({
  providedIn: 'root'
})
export class NpvApiService {
  private readonly baseUrl = `${environment.apiUrl}/Npv`;

  constructor(private http: HttpClient) {
  }

  calculateNpv(request: NpvRequest): Observable<CalculationResponse> {
    return this.http.post<CalculationResponse>(`${this.baseUrl}/calculate`, request);
  }

  getResults(calculationId: string): Observable<NpvCalculationResponse> {
    return this.http.get<NpvCalculationResponse>(`${this.baseUrl}/results/${calculationId}`);
  }

  getStatus(calculationId: string): Observable<NpvCalculationStatus> {
    return this.http.get<NpvCalculationStatus>(`${this.baseUrl}/status/${calculationId}`);
  }

  pollResults(calculationId: string, intervalMs: number = 1000): Observable<NpvCalculationResponse> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getResults(calculationId)),
      retryWhen(errors =>
        errors.pipe(
          delayWhen(() => timer(intervalMs))
        )
      )
    );
  }

  pollStatus(calculationId: string, intervalMs: number = 500): Observable<NpvCalculationStatus> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getStatus(calculationId)),
      retryWhen(errors =>
        errors.pipe(
          delayWhen(() => timer(intervalMs))
        )
      )
    );
  }


}
