import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GuestRegistrationContext } from '../../models/guest-registration.models';

@Component({
  selector: 'app-guest-registration-shell',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './guest-registration-shell.component.html',
  styleUrl: './guest-registration-shell.component.css'
})
export class GuestRegistrationShellComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
  readonly stepLabel = input<string>('');
  readonly context = input<GuestRegistrationContext | null>(null);
}
