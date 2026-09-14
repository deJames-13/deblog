import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { TelemetrySummaryDto } from '../models/api.dto';

@Injectable({
  providedIn: 'root',
})
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG, { optional: true });
  private readonly baseUrl = `${this.config?.apiUrl || '/api'}/admin/analytics`;

  get7DayTelemetry(days = 7): Observable<TelemetrySummaryDto> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<TelemetrySummaryDto>(`${this.baseUrl}/telemetry`, { params });
  }
}
