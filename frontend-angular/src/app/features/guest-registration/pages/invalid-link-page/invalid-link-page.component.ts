import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { GuestRegistrationShellComponent } from '../../components/guest-registration-shell/guest-registration-shell.component';

@Component({
  selector: 'app-invalid-link-page',
  standalone: true,
  imports: [CommonModule, GuestRegistrationShellComponent],
  templateUrl: './invalid-link-page.component.html'
})
export class InvalidLinkPageComponent {
  readonly message: string;

  constructor(route: ActivatedRoute) {
    this.message = route.snapshot.queryParamMap.get('message') ?? 'This registration link is invalid, expired, or no longer available.';
  }
}
