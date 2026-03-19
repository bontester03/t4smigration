export type Gender = 'Male' | 'Female' | 'Other';
export type ParentRelationship = 'Father' | 'Mother' | 'Other';
export type ConsentAnswerValue = 'Yes' | 'No';

export interface GuestRegistrationContext {
  code: string;
  schoolName: string | null;
  className: string | null;
  expiryDateUtc: string | null;
  isDisabled: boolean;
  isExpired: boolean;
  isValid: boolean;
  invalidReason: string | null;
}

export interface GuestConsentQuestion {
  consentQuestionId: number;
  questionText: string;
}

export interface GuestConsentAnswer {
  consentQuestionId: number;
  answer: ConsentAnswerValue;
}

export interface GuestParentFormValue {
  parentName: string;
  relationship: ParentRelationship;
  email: string;
  phoneNumber: string;
  postcode: string;
}

export interface GuestChildDetailsFormValue {
  childName: string;
  dateOfBirth: string;
  gender: Gender;
}

export interface GuestChildScore {
  physicalActivityScore: number;
  breakfastScore: number;
  fruitVegScore: number;
  sweetSnacksScore: number;
  fattyFoodsScore: number;
}

export interface GuestChildMedical {
  gpPracticeName: string;
  gpContactNumber: string;
  medicalConditions: string;
  allergies: string;
  medications: string;
  additionalNotes: string;
  isSensitive: boolean;
}

export interface GuestChildDraft extends GuestChildDetailsFormValue {
  score: GuestChildScore;
  medical: GuestChildMedical;
}

export interface GuestRegistrationDraft {
  code: string;
  context: GuestRegistrationContext | null;
  parent: GuestParentFormValue | null;
  currentChild: Partial<GuestChildDraft> | null;
  children: GuestChildDraft[];
  consentAnswers: GuestConsentAnswer[];
}

export interface GuestRegistrationSubmitRequest {
  code: string;
  parent: {
    parentName: string;
    email: string;
    phoneNumber: string;
    postcode: string;
    relationship: string;
  };
  children: Array<{
    childName: string;
    dateOfBirthUtc: string;
    gender: Gender;
    score: GuestChildScore;
    medical: GuestChildMedical;
  }>;
  consentAnswers: GuestConsentAnswer[];
}

export interface GuestRegistrationResult {
  success: boolean;
  message: string;
  redirectUrl: string | null;
  userId: string | null;
}
