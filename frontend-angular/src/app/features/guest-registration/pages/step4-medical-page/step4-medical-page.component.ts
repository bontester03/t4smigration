import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

@Component({
  selector: 'app-step4-medical-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, GuestRegistrationShellComponent],
  templateUrl: './step4-medical-page.component.html'
})
export class Step4MedicalPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly context = this.state.context;

  readonly form = this.fb.nonNullable.group({
    gpPracticeName: ['', [Validators.required, Validators.maxLength(100)]],
    gpContactNumber: ['', [Validators.required, Validators.maxLength(30)]],
    medicalConditions: ['', [Validators.maxLength(500)]],
    allergies: ['', [Validators.maxLength(300)]],
    medications: ['', [Validators.maxLength(300)]],
    additionalNotes: ['', [Validators.maxLength(500)]],
    isSensitive: false
  });

  constructor() {
    if (!this.state.workingChild()?.childName) {
      void this.router.navigate(['/guest-registration', this.code, 'step-2']);
      return;
    }

    if (!this.state.workingChild()?.score) {
      void this.router.navigate(['/guest-registration', this.code, 'step-3']);
      return;
    }

    const medical = this.state.workingChild()?.medical;
    if (medical) {
      this.form.patchValue(medical);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.commitCurrentChildMedical(this.form.getRawValue());
    void this.router.navigate(['/guest-registration', this.code, 'step-5']);
  }
}

