import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { ParentRelationship } from '../../models/guest-registration.models';
import { GuestRegistrationApiService } from '../../services/guest-registration-api.service';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

@Component({
  selector: 'app-step1-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, GuestRegistrationShellComponent],
  templateUrl: './step1-page.component.html'
})
export class Step1PageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(GuestRegistrationApiService);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly relationships: ParentRelationship[] = ['Father', 'Mother', 'Other'];
  readonly context = this.state.context;

  isLoading = true;
  isSubmitting = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    parentName: ['', [Validators.required, Validators.maxLength(100)]],
    relationship: ['' as ParentRelationship | '', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required]],
    postcode: ['', [Validators.required, Validators.maxLength(10)]]
  });

  constructor() {
    this.state.setCode(this.code);

    const parent = this.state.parent();
    if (parent) {
      this.form.patchValue(parent);
    }

    this.api.getContext(this.code)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (context) => {
          this.state.setContext(context);
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.invalidReason ?? 'This registration link is invalid.';
          void this.router.navigate(['/guest-registration/invalid'], { queryParams: { message } });
        }
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.state.saveParent(this.form.getRawValue() as never);
    void this.router.navigate(['/guest-registration', this.code, 'step-2']);
  }
}

