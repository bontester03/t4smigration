import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { RegistrationShellComponent } from '../../components/registration-shell/registration-shell.component';
import { RegistrationStateService } from '../../services/registration-state.service';

@Component({
  selector: 'app-registration-step4-medical-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RegistrationShellComponent],
  templateUrl: './step4-medical-page.component.html'
})
export class Step4MedicalPageComponent {
  private readonly fb = inject(FormBuilder);
  readonly router = inject(Router);
  private readonly state = inject(RegistrationStateService);

  readonly form = this.fb.nonNullable.group({
    gpPracticeName: ['', [Validators.required, Validators.maxLength(100)]],
    gpContactNumber: ['', [Validators.required]],
    medicalConditions: [''],
    allergies: [''],
    medications: [''],
    additionalNotes: ['']
  });

  constructor() {
    if (!this.state.child()) {
      void this.router.navigate(['/register/step-3']);
      return;
    }

    const medical = this.state.medical();
    if (medical) {
      this.form.patchValue(medical);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveMedical(this.form.getRawValue());
    void this.router.navigate(['/register/step-5']);
  }

  back(): void {
    void this.router.navigate(['/register/step-3']);
  }
}
