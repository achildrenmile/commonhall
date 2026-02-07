export type VisibilityRuleType = 'all' | 'groups' | 'rules';

export type RuleLogic = 'AND' | 'OR';

export type ConditionField =
  | 'department'
  | 'location'
  | 'jobTitle'
  | 'role'
  | 'preferredLanguage'
  | 'group';

export type ConditionOperator =
  | 'equals'
  | 'not_equals'
  | 'in'
  | 'not_in'
  | 'contains'
  | 'starts_with'
  | 'gte'
  | 'lte'
  | 'member_of'
  | 'not_member_of';

export interface RuleCondition {
  field: ConditionField;
  operator: ConditionOperator;
  value?: string;
  values?: string[];
}

export interface RuleSet {
  logic: RuleLogic;
  conditions: RuleCondition[];
}

export interface VisibilityRule {
  type: VisibilityRuleType;
  groupIds?: string[];
  rules?: RuleSet;
}

export interface TargetingPreview {
  totalCount: number;
  sampleUsers: Array<{
    id: string;
    displayName: string;
    department?: string;
    location?: string;
    avatarUrl?: string;
  }>;
}

export interface TargetingField {
  name: ConditionField;
  label: string;
  operators: ConditionOperator[];
  options?: string[];
  requiresGroupSelector?: boolean;
}

export interface TargetingSchema {
  fields: TargetingField[];
}

// Default empty rule
export const emptyRule: VisibilityRule = {
  type: 'all',
};

// Operator labels for display
export const operatorLabels: Record<ConditionOperator, string> = {
  equals: 'equals',
  not_equals: 'does not equal',
  in: 'is one of',
  not_in: 'is not one of',
  contains: 'contains',
  starts_with: 'starts with',
  gte: 'is greater than or equal to',
  lte: 'is less than or equal to',
  member_of: 'is a member of',
  not_member_of: 'is not a member of',
};

// Field labels for display
export const fieldLabels: Record<ConditionField, string> = {
  department: 'Department',
  location: 'Location',
  jobTitle: 'Job Title',
  role: 'Role',
  preferredLanguage: 'Preferred Language',
  group: 'Group',
};
