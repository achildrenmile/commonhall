export type SurveyType = 'OneTime' | 'Recurring';
export type SurveyStatus = 'Draft' | 'Active' | 'Closed' | 'Archived';
export type SurveyQuestionType = 'SingleChoice' | 'MultiChoice' | 'FreeText' | 'Rating' | 'NPS' | 'YesNo';

export interface SurveyQuestion {
  id: string;
  type: SurveyQuestionType;
  questionText: string;
  description?: string;
  options?: string; // JSON array
  isRequired: boolean;
  sortOrder: number;
  settings?: string;
}

export interface SurveyQuestionInput {
  id?: string;
  tempId?: string;
  type: SurveyQuestionType;
  questionText: string;
  description?: string;
  options?: string;
  isRequired?: boolean;
  settings?: string;
}

export interface SurveyListItem {
  id: string;
  title: string;
  description?: string;
  type: SurveyType;
  isAnonymous: boolean;
  status: SurveyStatus;
  startsAt?: string;
  endsAt?: string;
  questionCount: number;
  responseCount: number;
  createdAt: string;
}

export interface SurveyDetail {
  id: string;
  title: string;
  description?: string;
  type: SurveyType;
  recurrenceConfig?: string;
  isAnonymous: boolean;
  status: SurveyStatus;
  startsAt?: string;
  endsAt?: string;
  targetGroupIds?: string;
  spaceId?: string;
  spaceName?: string;
  questions: SurveyQuestion[];
  responseCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface SurveyAnalytics {
  surveyId: string;
  totalResponses: number;
  completeResponses: number;
  responseRate: number;
  questionAnalytics: QuestionAnalytics[];
}

export interface QuestionAnalytics {
  questionId: string;
  questionText: string;
  type: SurveyQuestionType;
  totalAnswers: number;
  analytics: ChoiceAnalytics | RatingAnalytics | FreeTextAnalytics;
}

export interface ChoiceAnalytics {
  options: Record<string, number>;
}

export interface RatingAnalytics {
  average: number;
  distribution: Record<number, number>;
  count: number;
}

export interface FreeTextAnalytics {
  responses: string[];
}

export interface CreateSurveyInput {
  title: string;
  description?: string;
  type?: SurveyType;
  recurrenceConfig?: string;
  isAnonymous?: boolean;
  startsAt?: string;
  endsAt?: string;
  targetGroupIds?: string;
  spaceId?: string;
}

export interface UpdateSurveyInput {
  title?: string;
  description?: string;
  type?: SurveyType;
  recurrenceConfig?: string;
  isAnonymous?: boolean;
  startsAt?: string;
  endsAt?: string;
  targetGroupIds?: string;
  spaceId?: string;
}
