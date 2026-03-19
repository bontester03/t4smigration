import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';
import { GuestRegistrationStateService } from '../../services/guest-registration-state.service';

@Component({
  selector: 'app-thank-you-page',
  standalone: true,
  imports: [CommonModule, GuestRegistrationShellComponent],
  templateUrl: './thank-you-page.component.html'
})
export class ThankYouPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly state = inject(GuestRegistrationStateService);

  readonly code = this.route.snapshot.paramMap.get('code') ?? '';
  readonly context = this.state.context;
  readonly childrenCount = computed(() => this.state.children().length);
}
