import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

interface ScoreOption {
  value: number;
  label: string;
}

@Component({
  selector: 'app-step3-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, GuestRegistrationShellComponent],
  templateUrl: './step3-page.component.html'
})
export class Step3PageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly context = this.state.context;
  readonly weeklyOptions: ScoreOption[] = [
    { value: 0, label: '0 days' },
    { value: 1, label: '1-2 days' },
    { value: 2, label: '3-4 days' },
    { value: 3, label: '5-6 days' },
    { value: 4, label: '7 days' }
  ];
  readonly fruitVegOptions: ScoreOption[] = [
    { value: 0, label: '0 portions' },
    { value: 1, label: '1-2 portions' },
    { value: 2, label: '3 portions' },
    { value: 3, label: '4 portions' },
    { value: 4, label: '5+ portions' }
  ];
  readonly snackOptions: ScoreOption[] = [
    { value: 0, label: '6+ times/week' },
    { value: 1, label: '3-5 times/week' },
    { value: 2, label: '1-2 times/week' },
    { value: 3, label: 'Less than 1/week' },
    { value: 4, label: 'Never' }
  ];

  readonly form = this.fb.nonNullable.group({
    physicalActivityScore: [-1, [Validators.required, Validators.min(0), Validators.max(4)]],
    breakfastScore: [-1, [Validators.required, Validators.min(0), Validators.max(4)]],
    fruitVegScore: [-1, [Validators.required, Validators.min(0), Validators.max(4)]],
    sweetSnacksScore: [-1, [Validators.required, Validators.min(0), Validators.max(4)]],
    fattyFoodsScore: [-1, [Validators.required, Validators.min(0), Validators.max(4)]]
  });

  constructor() {
    if (!this.state.parent()) {
      void this.router.navigate(['/guest-registration', this.code, 'step-1']);
      return;
    }

    if (!this.state.workingChild()?.childName) {
      void this.router.navigate(['/guest-registration', this.code, 'step-2']);
      return;
    }

    const score = this.state.workingChild()?.score;
    if (score) {
      this.form.patchValue(score);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveCurrentChildScore(this.form.getRawValue());
    void this.router.navigate(['/guest-registration', this.code, 'step-4']);
  }
}

