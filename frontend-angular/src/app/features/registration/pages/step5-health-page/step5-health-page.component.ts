import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { RegistrationShellComponent } from '../../components/registration-shell/registration-shell.component';
import { RegistrationStateService } from '../../services/registration-state.service';

@Component({
  selector: 'app-registration-step5-health-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RegistrationShellComponent],
  templateUrl: './step5-health-page.component.html'
})
export class Step5HealthPageComponent {
  private readonly fb = inject(FormBuilder);
  readonly router = inject(Router);
  private readonly state = inject(RegistrationStateService);

  readonly scoreOptions = [0, 1, 2, 3, 4];
  readonly scoreFields = [
    { controlName: 'physicalActivityScore', label: 'Physical Activity' },
    { controlName: 'breakfastScore', label: 'Breakfast' },
    { controlName: 'fruitVegScore', label: 'Fruit and Veg' },
    { controlName: 'sweetSnacksScore', label: 'Sweet Snacks' },
    { controlName: 'fattyFoodsScore', label: 'Fatty Foods' }
  ] as const;
  readonly form = this.fb.group({
    physicalActivityScore: [null as number | null, [Validators.required]],
    breakfastScore: [null as number | null, [Validators.required]],
    fruitVegScore: [null as number | null, [Validators.required]],
    sweetSnacksScore: [null as number | null, [Validators.required]],
    fattyFoodsScore: [null as number | null, [Validators.required]]
  });

  constructor() {
    if (!this.state.medical()) {
      void this.router.navigate(['/register/step-4']);
      return;
    }

    const healthScore = this.state.healthScore();
    if (healthScore) {
      this.form.patchValue(healthScore);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.saveHealthScore(this.form.getRawValue());
    void this.router.navigate(['/register/review']);
  }

  back(): void {
    void this.router.navigate(['/register/step-4']);
  }
}
