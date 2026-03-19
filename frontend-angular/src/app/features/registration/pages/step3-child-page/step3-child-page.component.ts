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
  selector: 'app-registration-step3-child-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RegistrationShellComponent],
  templateUrl: './step3-child-page.component.html'
})
export class Step3ChildPageComponent {
  private readonly fb = inject(FormBuilder);
  readonly router = inject(Router);
  private readonly api = inject(RegistrationApiService);
  private readonly state = inject(RegistrationStateService);

  readonly options = this.state.options;
  isLoading = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    childName: ['', [Validators.required, Validators.maxLength(100)]],
    gender: ['' as '' | 'Male' | 'Female' | 'Other', [Validators.required]],
    dateOfBirth: ['', [Validators.required]],
    school: ['', [Validators.required]],
    class: ['', [Validators.required]],
    avatarUrl: ['', [Validators.required]]
  });

  constructor() {
    if (!this.state.parent()) {
      void this.router.navigate(['/register/step-2']);
      return;
    }

    const child = this.state.child();
    if (child) {
      this.form.patchValue(child);
    }

    if (!this.options()) {
      this.isLoading = true;
      this.api.getOptions().pipe(finalize(() => (this.isLoading = false))).subscribe({
        next: (options) => {
          this.state.setOptions(options);
        },
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

    this.state.saveChild(this.form.getRawValue());
    void this.router.navigate(['/register/step-4']);
  }

  back(): void {
    void this.router.navigate(['/register/step-2']);
  }

  selectAvatar(avatarUrl: string): void {
    this.form.controls.avatarUrl.setValue(avatarUrl);
    this.form.controls.avatarUrl.markAsTouched();
    this.form.controls.avatarUrl.markAsDirty();
  }
}
