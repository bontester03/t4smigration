import { Routes } from '@angular/router';
import { InvalidLinkPageComponent } from './features/guest-registration/pages/invalid-link-page/invalid-link-page.component';
import { Step1PageComponent } from './features/guest-registration/pages/step1-page/step1-page.component';
import { Step2PageComponent } from './features/guest-registration/pages/step2-page/step2-page.component';
import { Step3PageComponent } from './features/guest-registration/pages/step3-page/step3-page.component';
import { Step4MedicalPageComponent } from './features/guest-registration/pages/step4-medical-page/step4-medical-page.component';
import { Step5PageComponent } from './features/guest-registration/pages/step5-page/step5-page.component';
import { ThankYouPageComponent } from './features/guest-registration/pages/thank-you-page/thank-you-page.component';
import { ReviewPageComponent } from './features/registration/pages/review-page/review-page.component';
import { Step1AccountPageComponent } from './features/registration/pages/step1-account-page/step1-account-page.component';
import { Step2ParentPageComponent } from './features/registration/pages/step2-parent-page/step2-parent-page.component';
import { Step3ChildPageComponent } from './features/registration/pages/step3-child-page/step3-child-page.component';
import { Step4MedicalPageComponent as RegistrationStep4MedicalPageComponent } from './features/registration/pages/step4-medical-page/step4-medical-page.component';
import { Step5HealthPageComponent } from './features/registration/pages/step5-health-page/step5-health-page.component';
import { SuccessPageComponent } from './features/registration/pages/success-page/success-page.component';

export const routes: Routes = [
  { path: '', redirectTo: 'register/step-1', pathMatch: 'full' },
  { path: 'register', redirectTo: 'register/step-1', pathMatch: 'full' },
  { path: 'register/step-1', component: Step1AccountPageComponent },
  { path: 'register/step-2', component: Step2ParentPageComponent },
  { path: 'register/step-3', component: Step3ChildPageComponent },
  { path: 'register/step-4', component: RegistrationStep4MedicalPageComponent },
  { path: 'register/step-5', component: Step5HealthPageComponent },
  { path: 'register/review', component: ReviewPageComponent },
  { path: 'register/success', component: SuccessPageComponent },
  { path: 'guest-registration/invalid', component: InvalidLinkPageComponent },
  { path: 'guest-registration/:code/step-1', component: Step1PageComponent },
  { path: 'guest-registration/:code/step-2', component: Step2PageComponent },
  { path: 'guest-registration/:code/step-3', component: Step3PageComponent },
  { path: 'guest-registration/:code/step-4', component: Step4MedicalPageComponent },
  { path: 'guest-registration/:code/step-5', component: Step5PageComponent },
  { path: 'guest-registration/:code/thank-you', component: ThankYouPageComponent },
  { path: '**', redirectTo: 'guest-registration/invalid' }
];
