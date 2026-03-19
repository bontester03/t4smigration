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
  selector: 'app-registration-step2-parent-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RegistrationShellComponent],
  templateUrl: './step2-parent-page.component.html'
})
export class Step2ParentPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(RegistrationApiService);
  private readonly state = inject(RegistrationStateService);

  readonly options = this.state.options;
  isLoading = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    parentGuardianName: ['', [Validators.required, Validators.maxLength(100)]],
    relationshipToChild: ['' as '' | 'Father' | 'Mother' | 'Guardian' | 'Other', [Validators.required]],
    teleNumber: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    postcode: ['', [Validators.required, Validators.maxLength(10)]]
  });

  constructor() {
    if (!this.state.account()) {
      void this.router.navigate(['/register/step-1']);
      return;
    }

    const parent = this.state.parent();
    if (parent) {
      this.form.patchValue(parent);
    } else {
      this.form.patchValue({ email: this.state.account()?.email ?? '' });
    }

    if (!this.options()) {
      this.isLoading = true;
      this.api.getOptions().pipe(finalize(() => (this.isLoading = false))).subscribe({
        next: (options) => this.state.setOptions(options),
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.message ?? 'Could not load registration options.';
        }
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveParent(this.form.getRawValue());
    void this.router.navigateByUrl('/register/step-3');
  }

  back(): void {
    void this.router.navigateByUrl('/register/step-1');
  }
}
