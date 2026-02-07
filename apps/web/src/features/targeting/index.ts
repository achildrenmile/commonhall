// Components
export { RuleBuilder } from './components';

// API hooks
export {
  useTargetingSchema,
  useTargetingPreview,
  useEvaluateRule,
  ruleToJson,
  jsonToRule,
} from './api';

// Types
export type {
  VisibilityRule,
  VisibilityRuleType,
  RuleLogic,
  ConditionField,
  ConditionOperator,
  RuleCondition,
  RuleSet,
  TargetingPreview,
  TargetingField,
  TargetingSchema,
} from './types';

export { emptyRule, operatorLabels, fieldLabels } from './types';
