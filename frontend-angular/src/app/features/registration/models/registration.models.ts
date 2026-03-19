export type RegistrationGender = 'Male' | 'Female' | 'Other';
export type RegistrationRelationship = 'Father' | 'Mother' | 'Guardian' | 'Other';

export interface RegistrationOption {
  id: number;
  name: string;
}

export interface RegistrationReferralType {
  id: number;
  name: string;
  category: string;
  requiresSchoolSelection: boolean;
}

export interface RegistrationOptions {
  referralTypes: RegistrationReferralType[];
  schools: RegistrationOption[];
  classes: RegistrationOption[];
  avatars: string[];
  relationships: RegistrationRelationship[];
  genders: RegistrationGender[];
}

export interface RegistrationAccountFormValue {
  email: string;
  password: string;
  referralTypeId: number | null;
}

export interface RegistrationParentFormValue {
  parentGuardianName: string;
  relationshipToChild: RegistrationRelationship | '';
  teleNumber: string;
  email: string;
  postcode: string;
}

export interface RegistrationChildFormValue {
  childName: string;
  gender: RegistrationGender | '';
  dateOfBirth: string;
  school: string;
  class: string;
  avatarUrl: string;
}

export interface RegistrationMedicalFormValue {
  gpPracticeName: string;
  gpContactNumber: string;
  medicalConditions: string;
  allergies: string;
  medications: string;
  additionalNotes: string;
}

export interface RegistrationHealthScoreFormValue {
  physicalActivityScore: number | null;
  breakfastScore: number | null;
  fruitVegScore: number | null;
  sweetSnacksScore: number | null;
  fattyFoodsScore: number | null;
}

export interface RegistrationDraft {
  options: RegistrationOptions | null;
  account: Omit<RegistrationAccountFormValue, 'password'> | null;
  parent: RegistrationParentFormValue | null;
  child: RegistrationChildFormValue | null;
  medical: RegistrationMedicalFormValue | null;
  healthScore: RegistrationHealthScoreFormValue | null;
}

export interface RegistrationSubmitRequest {
  account: {
    email: string;
    password: string;
    referralTypeId: number;
  };
  parent: {
    parentGuardianName: string;
    relationshipToChild: string;
    teleNumber: string;
    email: string;
    postcode: string;
  };
  child: {
    childName: string;
    gender: RegistrationGender;
    dateOfBirthUtc: string;
    school: string;
    class: string;
    avatarUrl: string;
  };
  medical: {
    gpPracticeName: string;
    gpContactNumber: string;
    medicalConditions: string | null;
    allergies: string | null;
    medications: string | null;
    additionalNotes: string | null;
  };
  healthScore: {
    physicalActivityScore: number;
    breakfastScore: number;
    fruitVegScore: number;
    sweetSnacksScore: number;
    fattyFoodsScore: number;
  };
}

export interface RegistrationResult {
  success: boolean;
  message: string;
  redirectUrl: string | null;
  userId: string | null;
}
