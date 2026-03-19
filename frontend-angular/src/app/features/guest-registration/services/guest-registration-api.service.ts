import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  GuestConsentQuestion,
  GuestRegistrationContext,
  GuestRegistrationResult,
  GuestRegistrationSubmitRequest
} from '../models/guest-registration.models';

@Injectable({ providedIn: 'root' })
export class GuestRegistrationApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getContext(code: string): Observable<GuestRegistrationContext> {
    return this.http.get<GuestRegistrationContext>(`${this.apiBaseUrl}/${encodeURIComponent(code)}/context`);
  }

  getConsentQuestions(): Observable<GuestConsentQuestion[]> {
    return this.http.get<GuestConsentQuestion[]>(`${this.apiBaseUrl}/consent-questions`);
  }

  submit(request: GuestRegistrationSubmitRequest): Observable<GuestRegistrationResult> {
    return this.http.post<GuestRegistrationResult>(`${this.apiBaseUrl}/submit`, request);
  }
}

