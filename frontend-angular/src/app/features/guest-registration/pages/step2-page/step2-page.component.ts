import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { Gender } from '../../models/guest-registration.models';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

function childAgeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string | null;
    if (!value) {
      return null;
    }

    const dob = new Date(value);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age--;
    }

    return age >= 2 && age <= 17 ? null : { invalidAge: true };
  };
}

@Component({
  selector: 'app-step2-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, GuestRegistrationShellComponent],
  templateUrl: './step2-page.component.html'
})
export class Step2PageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly context = this.state.context;
  readonly genders: Gender[] = ['Male', 'Female', 'Other'];

  readonly form = this.fb.nonNullable.group({
    childName: ['', [Validators.required]],
    dateOfBirth: ['', [Validators.required, childAgeValidator()]],
    gender: ['' as Gender | '', [Validators.required]]
  });

  constructor() {
    if (!this.state.parent()) {
      void this.router.navigate(['/guest-registration', this.code, 'step-1']);
      return;
    }

    const currentChild = this.state.workingChild();
    if (currentChild?.childName) {
      this.form.patchValue({
        childName: currentChild.childName ?? '',
        dateOfBirth: currentChild.dateOfBirth ?? '',
        gender: (currentChild.gender as Gender | '') ?? ''
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveCurrentChildDetails(this.form.getRawValue() as never);
    void this.router.navigate(['/guest-registration', this.code, 'step-3']);
  }
}

