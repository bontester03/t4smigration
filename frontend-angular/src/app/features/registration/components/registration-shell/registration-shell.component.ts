import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-registration-shell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './registration-shell.component.html'
})
export class RegistrationShellComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
  readonly stepLabel = input<string>('');
  readonly badgeText = input<string>('Join Us Today');
}
