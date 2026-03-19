import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RegistrationShellComponent } from '../../components/registration-shell/registration-shell.component';
import { RegistrationApiService } from '../../services/registration-api.service';
import { RegistrationStateService } from '../../services/registration-state.service';

@Component({
  selector: 'app-registration-review-page',
  standalone: true,
  imports: [CommonModule, RegistrationShellComponent],
  templateUrl: './review-page.component.html'
})
export class ReviewPageComponent {
  readonly router = inject(Router);
  private readonly api = inject(RegistrationApiService);
  private readonly state = inject(RegistrationStateService);

  readonly draft = this.state.draft;
  readonly options = this.state.options;
  isSubmitting = false;
  errorMessage = '';

  constructor() {
    if (!this.state.healthScore()) {
      void this.router.navigate(['/register/step-5']);
      return;
    }

    if (!this.state.passwordPresent()) {
      this.errorMessage = 'For security, re-enter your password to complete registration.';
      void this.router.navigate(['/register/step-1']);
    }
  }

  submit(): void {
    const request = this.state.buildSubmitRequest();
    if (!request) {
      this.errorMessage = 'Registration data is incomplete. Please review the previous steps.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.api.submit(request).subscribe({
      next: (result) => {
        this.state.reset();
        if (result.redirectUrl) {
          window.location.assign(result.redirectUrl);
          return;
        }

        void this.router.navigate(['/register/success']);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        this.errorMessage = error.error?.message ?? 'Registration could not be completed.';
      }
    });
  }

  back(): void {
    void this.router.navigate(['/register/step-5']);
  }

  referralTypeName(referralTypeId: number | null): string {
    if (referralTypeId == null) {
      return 'Not selected';
    }

    return this.options()?.referralTypes.find((item) => item.id === referralTypeId)?.name ?? `#${referralTypeId}`;
  }
}
