import { Injectable, computed, signal } from '@angular/core';
import {
  RegistrationAccountFormValue,
  RegistrationChildFormValue,
  RegistrationDraft,
  RegistrationHealthScoreFormValue,
  RegistrationMedicalFormValue,
  RegistrationOptions,
  RegistrationParentFormValue,
  RegistrationSubmitRequest
} from '../models/registration.models';

const STORAGE_KEY = 'time4wellbeing-registration-v1';

function createEmptyDraft(): RegistrationDraft {
  return {
    options: null,
    account: null,
    parent: null,
    child: null,
    medical: null,
    healthScore: null
  };
}

@Injectable({ providedIn: 'root' })
export class RegistrationStateService {
  private readonly draftSignal = signal<RegistrationDraft>(this.loadInitialDraft());
  private readonly passwordSignal = signal<string>('');

  readonly draft = this.draftSignal.asReadonly();
  readonly options = computed(() => this.draftSignal().options);
  readonly account = computed(() => this.draftSignal().account);
  readonly parent = computed(() => this.draftSignal().parent);
  readonly child = computed(() => this.draftSignal().child);
  readonly medical = computed(() => this.draftSignal().medical);
  readonly healthScore = computed(() => this.draftSignal().healthScore);
  readonly passwordPresent = computed(() => this.passwordSignal().length > 0);

  setOptions(options: RegistrationOptions): void {
    this.writeDraft((draft) => ({ ...draft, options }));
  }

  saveAccount(account: RegistrationAccountFormValue): void {
    this.passwordSignal.set(account.password);
    this.writeDraft((draft) => ({
      ...draft,
      account: {
        email: account.email,
        referralTypeId: account.referralTypeId
      },
      parent: draft.parent ? { ...draft.parent, email: account.email } : draft.parent
    }));
  }

  saveParent(parent: RegistrationParentFormValue): void {
    this.writeDraft((draft) => ({ ...draft, parent }));
  }

  saveChild(child: RegistrationChildFormValue): void {
    this.writeDraft((draft) => ({ ...draft, child }));
  }

  saveMedical(medical: RegistrationMedicalFormValue): void {
    this.writeDraft((draft) => ({ ...draft, medical }));
  }

  saveHealthScore(healthScore: RegistrationHealthScoreFormValue): void {
    this.writeDraft((draft) => ({ ...draft, healthScore }));
  }

  reset(): void {
    this.passwordSignal.set('');
    this.writeDraft(() => createEmptyDraft());
  }

  buildSubmitRequest(): RegistrationSubmitRequest | null {
    const draft = this.draftSignal();
    const password = this.passwordSignal();

    if (!draft.account || !password || !draft.parent || !draft.child || !draft.medical || !draft.healthScore) {
      return null;
    }

    if (draft.account.referralTypeId == null || Object.values(draft.healthScore).some((value) => value == null)) {
      return null;
    }

    return {
      account: {
        email: draft.account.email,
        password,
        referralTypeId: draft.account.referralTypeId
      },
      parent: {
        parentGuardianName: draft.parent.parentGuardianName,
        relationshipToChild: draft.parent.relationshipToChild,
        teleNumber: draft.parent.teleNumber,
        email: draft.parent.email,
        postcode: draft.parent.postcode
      },
      child: {
        childName: draft.child.childName,
        gender: draft.child.gender as 'Male' | 'Female' | 'Other',
        dateOfBirthUtc: `${draft.child.dateOfBirth}T00:00:00Z`,
        school: draft.child.school,
        class: draft.child.class,
        avatarUrl: draft.child.avatarUrl
      },
      medical: {
        gpPracticeName: draft.medical.gpPracticeName,
        gpContactNumber: draft.medical.gpContactNumber,
        medicalConditions: draft.medical.medicalConditions || null,
        allergies: draft.medical.allergies || null,
        medications: draft.medical.medications || null,
        additionalNotes: draft.medical.additionalNotes || null
      },
      healthScore: {
        physicalActivityScore: draft.healthScore.physicalActivityScore!,
        breakfastScore: draft.healthScore.breakfastScore!,
        fruitVegScore: draft.healthScore.fruitVegScore!,
        sweetSnacksScore: draft.healthScore.sweetSnacksScore!,
        fattyFoodsScore: draft.healthScore.fattyFoodsScore!
      }
    };
  }

  private loadInitialDraft(): RegistrationDraft {
    if (typeof sessionStorage === 'undefined') {
      return createEmptyDraft();
    }

    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return createEmptyDraft();
    }

    try {
      return JSON.parse(raw) as RegistrationDraft;
    } catch {
      sessionStorage.removeItem(STORAGE_KEY);
      return createEmptyDraft();
    }
  }

  private writeDraft(update: (draft: RegistrationDraft) => RegistrationDraft): void {
    const next = update(this.draftSignal());
    this.draftSignal.set(next);

    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    }
  }
}
