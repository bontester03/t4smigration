import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { UntypedFormArray, UntypedFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { GuestConsentQuestion } from '../../models/guest-registration.models';
import { GuestRegistrationApiService } from '../../services/guest-registration-api.service';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

@Component({
  selector: 'app-step5-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, GuestRegistrationShellComponent],
  templateUrl: './step5-page.component.html'
})
export class Step5PageComponent {
  private readonly fb = inject(UntypedFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(GuestRegistrationApiService);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly context = this.state.context;
  readonly children = this.state.children;

  isLoading = true;
  isSubmitting = false;
  errorMessage = '';
  questions: GuestConsentQuestion[] = [];

  readonly form = this.fb.group({
    consentAnswers: this.fb.array([])
  });

  get consentAnswersArray(): UntypedFormArray {
    return this.form.get('consentAnswers') as UntypedFormArray;
  }

  constructor() {
    if (this.state.children().length === 0) {
      void this.router.navigate(['/guest-registration', this.code, 'step-2']);
      return;
    }

    this.api.getConsentQuestions()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (questions) => {
          this.questions = questions;
          const savedAnswers = this.state.consentAnswers();
          const answersArray = this.fb.array(
            questions.map((question) => {
              const savedAnswer = savedAnswers.find((answer) => answer.consentQuestionId === question.consentQuestionId);
              return this.fb.group({
                consentQuestionId: [question.consentQuestionId, Validators.required],
                answer: [savedAnswer?.answer ?? '', Validators.required]
              });
            })
          );

          this.form.setControl('consentAnswers', answersArray);
        },
        error: () => {
          this.errorMessage = 'Consent questions could not be loaded.';
        }
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.persistAnswers();
    if (!request) {
      this.errorMessage = 'The registration data is incomplete. Please review the earlier steps.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.api.submit(request)
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          void this.router.navigate(['/guest-registration', this.code, 'thank-you']);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.message ?? 'Registration submission failed.';
        }
      });
  }

  private persistAnswers() {
    const answers = this.consentAnswersArray.getRawValue();
    this.state.saveConsentAnswers(answers);
    return this.state.buildSubmitRequest();
  }
}

