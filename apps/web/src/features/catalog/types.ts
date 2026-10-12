export interface SubjectDto {
  id: string;
  code: string;
  displayName: string;
}

export interface SpecialtyDto {
  id: string;
  subjectId: string;
  code: string;
  displayName: string;
}

export interface LanguageDto {
  code: string;
  displayName: string;
}
