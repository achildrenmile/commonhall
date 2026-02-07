'use client';

import { useState, useCallback, useMemo } from 'react';
import {
  Plus,
  Trash2,
  Users,
  Globe,
  Settings2,
  ChevronDown,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import { cn } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';
import { useTargetingPreview, ruleToJson } from '../api';
import type {
  VisibilityRule,
  VisibilityRuleType,
  RuleLogic,
  RuleCondition,
  ConditionField,
  ConditionOperator,
} from '../types';
import { operatorLabels, fieldLabels } from '../types';

interface RuleBuilderProps {
  value: VisibilityRule;
  onChange: (rule: VisibilityRule) => void;
  groups?: Array<{ id: string; name: string }>;
  className?: string;
}

// Field configuration with available operators
const fieldConfig: Record<ConditionField, { operators: ConditionOperator[]; options?: string[] }> = {
  department: {
    operators: ['equals', 'not_equals', 'in', 'not_in', 'contains', 'starts_with'],
  },
  location: {
    operators: ['equals', 'not_equals', 'in', 'not_in', 'contains', 'starts_with'],
  },
  jobTitle: {
    operators: ['equals', 'not_equals', 'in', 'not_in', 'contains', 'starts_with'],
  },
  role: {
    operators: ['equals', 'not_equals', 'in', 'not_in'],
    options: ['Employee', 'Editor', 'Admin'],
  },
  preferredLanguage: {
    operators: ['equals', 'not_equals', 'in', 'not_in'],
  },
  group: {
    operators: ['member_of', 'not_member_of'],
  },
};

function ConditionRow({
  condition,
  index,
  groups,
  onChange,
  onRemove,
}: {
  condition: RuleCondition;
  index: number;
  groups?: Array<{ id: string; name: string }>;
  onChange: (condition: RuleCondition) => void;
  onRemove: () => void;
}) {
  const config = fieldConfig[condition.field];
  const isGroupField = condition.field === 'group';
  const hasOptions = config.options && config.options.length > 0;
  const isMultiValue = ['in', 'not_in', 'member_of', 'not_member_of'].includes(condition.operator);

  const handleFieldChange = (field: ConditionField) => {
    const newConfig = fieldConfig[field];
    onChange({
      ...condition,
      field,
      operator: newConfig.operators[0],
      value: undefined,
      values: undefined,
    });
  };

  const handleOperatorChange = (operator: ConditionOperator) => {
    const newIsMulti = ['in', 'not_in', 'member_of', 'not_member_of'].includes(operator);
    const wasMulti = isMultiValue;

    if (newIsMulti !== wasMulti) {
      onChange({
        ...condition,
        operator,
        value: newIsMulti ? undefined : condition.values?.[0],
        values: newIsMulti ? (condition.value ? [condition.value] : []) : undefined,
      });
    } else {
      onChange({ ...condition, operator });
    }
  };

  const handleValueChange = (value: string) => {
    onChange({ ...condition, value, values: undefined });
  };

  const handleValuesChange = (values: string[]) => {
    onChange({ ...condition, values, value: undefined });
  };

  const addValue = (value: string) => {
    if (!value.trim()) return;
    const current = condition.values || [];
    if (!current.includes(value)) {
      handleValuesChange([...current, value]);
    }
  };

  const removeValue = (value: string) => {
    handleValuesChange((condition.values || []).filter((v) => v !== value));
  };

  return (
    <div className="flex items-start gap-2 p-3 bg-slate-50 dark:bg-slate-900 rounded-lg">
      {/* Field selector */}
      <Select value={condition.field} onValueChange={handleFieldChange}>
        <SelectTrigger className="w-36">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {Object.keys(fieldConfig).map((field) => (
            <SelectItem key={field} value={field}>
              {fieldLabels[field as ConditionField]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* Operator selector */}
      <Select value={condition.operator} onValueChange={handleOperatorChange}>
        <SelectTrigger className="w-40">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {config.operators.map((op) => (
            <SelectItem key={op} value={op}>
              {operatorLabels[op]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* Value input */}
      <div className="flex-1">
        {isGroupField && groups ? (
          <GroupSelector
            value={condition.values || (condition.value ? [condition.value] : [])}
            onChange={handleValuesChange}
            groups={groups}
            multiple={isMultiValue}
          />
        ) : isMultiValue ? (
          <div className="space-y-2">
            <div className="flex flex-wrap gap-1">
              {(condition.values || []).map((val) => (
                <Badge key={val} variant="secondary" className="gap-1">
                  {val}
                  <button onClick={() => removeValue(val)} className="ml-1">
                    <Trash2 className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
            </div>
            {hasOptions ? (
              <Select onValueChange={(v) => addValue(v)}>
                <SelectTrigger>
                  <SelectValue placeholder="Add value..." />
                </SelectTrigger>
                <SelectContent>
                  {config.options!
                    .filter((opt) => !(condition.values || []).includes(opt))
                    .map((opt) => (
                      <SelectItem key={opt} value={opt}>
                        {opt}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
            ) : (
              <Input
                placeholder="Add value and press Enter..."
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    addValue((e.target as HTMLInputElement).value);
                    (e.target as HTMLInputElement).value = '';
                  }
                }}
              />
            )}
          </div>
        ) : hasOptions ? (
          <Select value={condition.value || ''} onValueChange={handleValueChange}>
            <SelectTrigger>
              <SelectValue placeholder="Select value..." />
            </SelectTrigger>
            <SelectContent>
              {config.options!.map((opt) => (
                <SelectItem key={opt} value={opt}>
                  {opt}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <Input
            value={condition.value || ''}
            onChange={(e) => handleValueChange(e.target.value)}
            placeholder="Enter value..."
          />
        )}
      </div>

      {/* Remove button */}
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8 text-red-500 hover:text-red-600 shrink-0"
        onClick={onRemove}
      >
        <Trash2 className="h-4 w-4" />
      </Button>
    </div>
  );
}

function GroupSelector({
  value,
  onChange,
  groups,
  multiple,
}: {
  value: string[];
  onChange: (value: string[]) => void;
  groups: Array<{ id: string; name: string }>;
  multiple: boolean;
}) {
  const [open, setOpen] = useState(false);

  const selectedGroups = groups.filter((g) => value.includes(g.id));

  const toggleGroup = (groupId: string) => {
    if (value.includes(groupId)) {
      onChange(value.filter((id) => id !== groupId));
    } else if (multiple) {
      onChange([...value, groupId]);
    } else {
      onChange([groupId]);
    }
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className="w-full justify-between">
          {selectedGroups.length > 0 ? (
            <div className="flex flex-wrap gap-1">
              {selectedGroups.map((g) => (
                <Badge key={g.id} variant="secondary" className="text-xs">
                  {g.name}
                </Badge>
              ))}
            </div>
          ) : (
            <span className="text-slate-500">Select groups...</span>
          )}
          <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="p-0 w-64" align="start">
        <Command>
          <CommandInput placeholder="Search groups..." />
          <CommandList>
            <CommandEmpty>No groups found.</CommandEmpty>
            <CommandGroup>
              {groups.map((group) => (
                <CommandItem
                  key={group.id}
                  onSelect={() => toggleGroup(group.id)}
                  className="flex items-center gap-2"
                >
                  <div
                    className={cn(
                      'h-4 w-4 border rounded flex items-center justify-center',
                      value.includes(group.id) && 'bg-slate-900 dark:bg-slate-100'
                    )}
                  >
                    {value.includes(group.id) && (
                      <span className="text-white dark:text-slate-900 text-xs">✓</span>
                    )}
                  </div>
                  {group.name}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

function PreviewButton({ rule }: { rule: VisibilityRule }) {
  const [showPreview, setShowPreview] = useState(false);
  const ruleJson = useMemo(() => ruleToJson(rule), [rule]);
  const debouncedRuleJson = useDebounce(ruleJson, 500);

  const { data: preview, isLoading } = useTargetingPreview(debouncedRuleJson, showPreview);

  return (
    <Popover open={showPreview} onOpenChange={setShowPreview}>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2">
          <Users className="h-4 w-4" />
          Preview
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-80" align="end">
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h4 className="font-medium">Matching Users</h4>
            {isLoading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Badge variant="secondary">{preview?.totalCount ?? 0} users</Badge>
            )}
          </div>

          {preview && preview.sampleUsers.length > 0 ? (
            <div className="space-y-2">
              {preview.sampleUsers.map((user) => (
                <div key={user.id} className="flex items-center gap-2">
                  <Avatar className="h-6 w-6">
                    <AvatarImage src={user.avatarUrl} />
                    <AvatarFallback className="text-xs">
                      {user.displayName?.[0] || '?'}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{user.displayName}</p>
                    <p className="text-xs text-slate-500 truncate">
                      {[user.department, user.location].filter(Boolean).join(' • ')}
                    </p>
                  </div>
                </div>
              ))}
              {preview.totalCount > preview.sampleUsers.length && (
                <p className="text-xs text-slate-500 text-center">
                  and {preview.totalCount - preview.sampleUsers.length} more...
                </p>
              )}
            </div>
          ) : !isLoading ? (
            <div className="text-center py-4 text-slate-500">
              <AlertCircle className="h-8 w-8 mx-auto mb-2 opacity-50" />
              <p className="text-sm">No matching users</p>
            </div>
          ) : null}
        </div>
      </PopoverContent>
    </Popover>
  );
}

export function RuleBuilder({ value, onChange, groups = [], className }: RuleBuilderProps) {
  const handleTypeChange = (type: VisibilityRuleType) => {
    if (type === 'all') {
      onChange({ type: 'all' });
    } else if (type === 'groups') {
      onChange({ type: 'groups', groupIds: value.groupIds || [] });
    } else {
      onChange({
        type: 'rules',
        rules: value.rules || { logic: 'AND', conditions: [] },
      });
    }
  };

  const handleGroupsChange = (groupIds: string[]) => {
    onChange({ ...value, type: 'groups', groupIds });
  };

  const handleLogicChange = (logic: RuleLogic) => {
    if (value.type !== 'rules') return;
    onChange({
      ...value,
      rules: { ...value.rules!, logic },
    });
  };

  const handleConditionChange = (index: number, condition: RuleCondition) => {
    if (value.type !== 'rules' || !value.rules) return;
    const newConditions = [...value.rules.conditions];
    newConditions[index] = condition;
    onChange({
      ...value,
      rules: { ...value.rules, conditions: newConditions },
    });
  };

  const handleAddCondition = () => {
    if (value.type !== 'rules') return;
    const currentRules = value.rules || { logic: 'AND', conditions: [] };
    onChange({
      ...value,
      rules: {
        ...currentRules,
        conditions: [
          ...currentRules.conditions,
          { field: 'department', operator: 'equals', value: '' },
        ],
      },
    });
  };

  const handleRemoveCondition = (index: number) => {
    if (value.type !== 'rules' || !value.rules) return;
    const newConditions = value.rules.conditions.filter((_, i) => i !== index);
    onChange({
      ...value,
      rules: { ...value.rules, conditions: newConditions },
    });
  };

  return (
    <div className={cn('space-y-4', className)}>
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">Visible to</Label>
        <PreviewButton rule={value} />
      </div>

      {/* Type selector */}
      <ToggleGroup
        type="single"
        value={value.type}
        onValueChange={(v) => v && handleTypeChange(v as VisibilityRuleType)}
        className="justify-start"
      >
        <ToggleGroupItem value="all" className="gap-2">
          <Globe className="h-4 w-4" />
          Everyone
        </ToggleGroupItem>
        <ToggleGroupItem value="groups" className="gap-2">
          <Users className="h-4 w-4" />
          Groups
        </ToggleGroupItem>
        <ToggleGroupItem value="rules" className="gap-2">
          <Settings2 className="h-4 w-4" />
          Custom Rules
        </ToggleGroupItem>
      </ToggleGroup>

      {/* Groups selector */}
      {value.type === 'groups' && (
        <div className="space-y-2">
          <Label className="text-sm text-slate-500">Select groups that can see this content</Label>
          <GroupSelector
            value={value.groupIds || []}
            onChange={handleGroupsChange}
            groups={groups}
            multiple
          />
        </div>
      )}

      {/* Custom rules */}
      {value.type === 'rules' && (
        <div className="space-y-3">
          {/* Logic toggle */}
          {value.rules && value.rules.conditions.length > 1 && (
            <div className="flex items-center gap-2">
              <span className="text-sm text-slate-500">Match</span>
              <ToggleGroup
                type="single"
                value={value.rules.logic}
                onValueChange={(v) => v && handleLogicChange(v as RuleLogic)}
                size="sm"
              >
                <ToggleGroupItem value="AND">All conditions</ToggleGroupItem>
                <ToggleGroupItem value="OR">Any condition</ToggleGroupItem>
              </ToggleGroup>
            </div>
          )}

          {/* Conditions */}
          <div className="space-y-2">
            {value.rules?.conditions.map((condition, index) => (
              <ConditionRow
                key={index}
                condition={condition}
                index={index}
                groups={groups}
                onChange={(c) => handleConditionChange(index, c)}
                onRemove={() => handleRemoveCondition(index)}
              />
            ))}
          </div>

          {/* Add condition button */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleAddCondition}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Condition
          </Button>
        </div>
      )}
    </div>
  );
}
