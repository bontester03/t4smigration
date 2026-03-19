import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RegistrationOptions, RegistrationResult, RegistrationSubmitRequest } from '../models/registration.models';

@Injectable({ providedIn: 'root' })
export class RegistrationApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.registrationApiBaseUrl;

  getOptions(): Observable<RegistrationOptions> {
    return this.http.get<RegistrationOptions>(`${this.apiBaseUrl}/options`);
  }

  submit(request: RegistrationSubmitRequest): Observable<RegistrationResult> {
    return this.http.post<RegistrationResult>(`${this.apiBaseUrl}/submit`, request);
  }
}
