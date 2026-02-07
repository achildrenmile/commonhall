export type JourneyTriggerType = 'Manual' | 'Onboarding' | 'RoleChange' | 'LocationChange' | 'DateBased' | 'GroupJoin';
export type JourneyChannelType = 'AppNotification' | 'Email' | 'Both';
export type JourneyEnrollmentStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled';

export interface Journey {
  id: string;
  name: string;
  description?: string;
  triggerType: JourneyTriggerType;
  isActive: boolean;
  stepCount: number;
  enrollmentCount: number;
  completionRate: number;
  createdAt: string;
  updatedAt: string;
}

export interface JourneyDetail {
  id: string;
  name: string;
  description?: string;
  triggerType: JourneyTriggerType;
  triggerConfig?: string;
  isActive: boolean;
  spaceId?: string;
  spaceName?: string;
  steps: JourneyStep[];
  enrollmentCount: number;
  activeEnrollments: number;
  completedEnrollments: number;
  createdAt: string;
  updatedAt: string;
}

export interface JourneyStep {
  id: string;
  sortOrder: number;
  title: string;
  description?: string;
  content: string;
  delayDays: number;
  channelType: JourneyChannelType;
  isRequired: boolean;
}

export interface JourneyStepInput {
  id?: string;
  title: string;
  description?: string;
  content?: string;
  delayDays?: number;
  channelType?: JourneyChannelType;
  isRequired?: boolean;
}

export interface JourneyEnrollment {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  status: JourneyEnrollmentStatus;
  currentStepIndex: number;
  stepsCompleted: number;
  startedAt: string;
  completedAt?: string;
}

export interface JourneyAnalytics {
  totalEnrollments: number;
  activeEnrollments: number;
  completedEnrollments: number;
  cancelledEnrollments: number;
  completionRate: number;
  averageCompletionDays: number;
  stepFunnel: StepFunnelItem[];
  enrollmentTimeline: EnrollmentTimelineItem[];
}

export interface StepFunnelItem {
  stepIndex: number;
  stepTitle: string;
  delivered: number;
  completed: number;
  completionRate: number;
}

export interface EnrollmentTimelineItem {
  date: string;
  newEnrollments: number;
  completions: number;
}

export interface TriggerConfig {
  targetRoles?: string[];
  targetLocations?: string[];
  targetGroupIds?: string[];
  dateField?: string;
  daysOffset?: number;
}

export interface CreateJourneyInput {
  name: string;
  description?: string;
  triggerType?: JourneyTriggerType;
  triggerConfig?: string;
  spaceId?: string;
}

export interface UpdateJourneyInput {
  name?: string;
  description?: string;
  triggerType?: JourneyTriggerType;
  triggerConfig?: string;
  spaceId?: string;
}

// User-facing types
export interface MyJourney {
  enrollmentId: string;
  journeyId: string;
  journeyName: string;
  journeyDescription?: string;
  status: JourneyEnrollmentStatus;
  totalSteps: number;
  completedSteps: number;
  currentStepIndex: number;
  progressPercent: number;
  startedAt: string;
  completedAt?: string;
}

export interface MyJourneyDetail extends MyJourney {
  steps: MyJourneyStep[];
}

export interface MyJourneyStep {
  stepIndex: number;
  title: string;
  description?: string;
  content: string;
  delayDays: number;
  isRequired: boolean;
  isDelivered: boolean;
  isCompleted: boolean;
  isCurrentStep: boolean;
  deliveredAt?: string;
  completedAt?: string;
  viewedAt?: string;
}
