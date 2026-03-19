import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RegistrationShellComponent } from '../../components/registration-shell/registration-shell.component';

@Component({
  selector: 'app-registration-success-page',
  standalone: true,
  imports: [CommonModule, RegistrationShellComponent],
  templateUrl: './success-page.component.html'
})
export class SuccessPageComponent {}
