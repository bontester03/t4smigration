import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { RegistrationShellComponent } from '../../components/registration-shell/registration-shell.component';
import { RegistrationApiService } from '../../services/registration-api.service';
import { RegistrationStateService } from '../../services/registration-state.service';

@Component({
  selector: 'app-registration-step1-account-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RegistrationShellComponent],
  templateUrl: './step1-account-page.component.html'
})
export class Step1AccountPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(RegistrationApiService);
  private readonly state = inject(RegistrationStateService);

  readonly options = this.state.options;
  isLoading = true;
  isSubmitting = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    referralTypeId: [null as number | null, [Validators.required]]
  });

  constructor() {
    const account = this.state.account();
    if (account) {
      this.form.patchValue(account);
    }

    this.api.getOptions()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (options) => this.state.setOptions(options),
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.message ?? 'Could not load registration options.';
        }
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveAccount(this.form.getRawValue());
    void this.router.navigate(['/register/step-2']);
  }
}
