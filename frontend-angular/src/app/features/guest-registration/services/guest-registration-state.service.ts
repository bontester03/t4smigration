import { Injectable, computed, signal } from '@angular/core';
import {
  GuestChildDetailsFormValue,
  GuestChildDraft,
  GuestChildMedical,
  GuestChildScore,
  GuestConsentAnswer,
  GuestParentFormValue,
  GuestRegistrationContext,
  GuestRegistrationDraft,
  GuestRegistrationSubmitRequest
} from '../models/guest-registration.models';

const STORAGE_KEY = 'time4wellbeing-guest-registration-v1';

function createEmptyDraft(code = ''): GuestRegistrationDraft {
  return {
    code,
    context: null,
    parent: null,
    currentChild: null,
    children: [],
    consentAnswers: []
  };
}

@Injectable({ providedIn: 'root' })
export class GuestRegistrationStateService {
  private readonly draftSignal = signal<GuestRegistrationDraft>(this.loadInitialDraft());

  readonly draft = this.draftSignal.asReadonly();
  readonly context = computed(() => this.draftSignal().context);
  readonly parent = computed(() => this.draftSignal().parent);
  readonly currentChild = computed(() => this.draftSignal().currentChild);
  readonly children = computed(() => this.draftSignal().children);
  readonly consentAnswers = computed(() => this.draftSignal().consentAnswers);
  readonly workingChild = computed(() => this.draftSignal().currentChild ?? this.draftSignal().children[0] ?? null);

  setCode(code: string): void {
    if (this.draftSignal().code === code) {
      return;
    }

    this.writeDraft(() => createEmptyDraft(code));
  }

  setContext(context: GuestRegistrationContext): void {
    this.writeDraft((draft) => ({
      ...draft,
      code: context.code,
      context
    }));
  }

  saveParent(parent: GuestParentFormValue): void {
    this.writeDraft((draft) => ({
      ...draft,
      parent
    }));
  }

  saveCurrentChildDetails(details: GuestChildDetailsFormValue): void {
    this.writeDraft((draft) => ({
      ...draft,
      currentChild: {
        ...(draft.currentChild ?? draft.children[0] ?? {}),
        ...details
      }
    }));
  }

  saveCurrentChildScore(score: GuestChildScore): void {
    this.writeDraft((draft) => ({
      ...draft,
      currentChild: {
        ...(draft.currentChild ?? draft.children[0] ?? {}),
        score
      }
    }));
  }

  commitCurrentChildMedical(medical: GuestChildMedical): void {
    this.writeDraft((draft) => {
      const currentChild = {
        ...(draft.currentChild ?? {}),
        medical
      } as GuestChildDraft;

      return {
        ...draft,
        currentChild: null,
        children: [currentChild]
      };
    });
  }

  saveConsentAnswers(consentAnswers: GuestConsentAnswer[]): void {
    this.writeDraft((draft) => ({
      ...draft,
      consentAnswers
    }));
  }

  reset(code = this.draftSignal().code): void {
    this.writeDraft(() => createEmptyDraft(code));
  }

  buildSubmitRequest(): GuestRegistrationSubmitRequest | null {
    const draft = this.draftSignal();

    if (!draft.code || !draft.parent || draft.children.length === 0) {
      return null;
    }

    return {
      code: draft.code,
      parent: {
        parentName: draft.parent.parentName,
        email: draft.parent.email,
        phoneNumber: draft.parent.phoneNumber,
        postcode: draft.parent.postcode,
        relationship: draft.parent.relationship
      },
      children: draft.children.map((child) => ({
        childName: child.childName,
        dateOfBirthUtc: `${child.dateOfBirth}T00:00:00Z`,
        gender: child.gender,
        score: child.score,
        medical: child.medical
      })),
      consentAnswers: draft.consentAnswers
    };
  }

  private loadInitialDraft(): GuestRegistrationDraft {
    if (typeof sessionStorage === 'undefined') {
      return createEmptyDraft();
    }

    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return createEmptyDraft();
    }

    try {
      return JSON.parse(raw) as GuestRegistrationDraft;
    } catch {
      sessionStorage.removeItem(STORAGE_KEY);
      return createEmptyDraft();
    }
  }

  private writeDraft(update: (draft: GuestRegistrationDraft) => GuestRegistrationDraft): void {
    const next = update(this.draftSignal());
    this.draftSignal.set(next);

    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    }
  }
}
