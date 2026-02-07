'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import {
  Save,
  Play,
  Pause,
  ChevronLeft,
  Plus,
  GripVertical,
  Trash2,
  Settings,
  ChevronDown,
  ChevronUp,
  Loader2,
  Mail,
  Bell,
  Clock,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { Separator } from '@/components/ui/separator';
import { useToast } from '@/hooks/use-toast';
import { EmailBlockEditor } from '@/features/email/components';
import {
  useJourney,
  useCreateJourney,
  useUpdateJourney,
  useUpdateJourneySteps,
  useActivateJourney,
  useDeactivateJourney,
} from '../api';
import type { JourneyStep, JourneyStepInput, JourneyTriggerType, JourneyChannelType, TriggerConfig } from '../types';
import type { EmailBlock } from '@/features/email/types';

interface JourneyBuilderProps {
  id: string;
}

const triggerTypeLabels: Record<JourneyTriggerType, string> = {
  Manual: 'Manual Enrollment',
  Onboarding: 'New Employee Onboarding',
  RoleChange: 'Role/Job Title Change',
  LocationChange: 'Location Change',
  DateBased: 'Date-Based (Anniversary, etc.)',
  GroupJoin: 'Group Membership',
};

const channelTypeLabels: Record<JourneyChannelType, string> = {
  AppNotification: 'App Notification',
  Email: 'Email Only',
  Both: 'Email + Notification',
};

function generateId(): string {
  return `step-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

interface StepEditorProps {
  step: JourneyStepInput & { tempId?: string };
  index: number;
  totalSteps: number;
  isExpanded: boolean;
  onToggle: () => void;
  onUpdate: (updates: Partial<JourneyStepInput>) => void;
  onDelete: () => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
}

function StepEditor({
  step,
  index,
  totalSteps,
  isExpanded,
  onToggle,
  onUpdate,
  onDelete,
  onMoveUp,
  onMoveDown,
}: StepEditorProps) {
  const [blocks, setBlocks] = useState<EmailBlock[]>(() => {
    try {
      return JSON.parse(step.content || '[]');
    } catch {
      return [];
    }
  });

  useEffect(() => {
    onUpdate({ content: JSON.stringify(blocks) });
  }, [blocks]);

  return (
    <Collapsible open={isExpanded} onOpenChange={onToggle}>
      <div className="border rounded-lg bg-white dark:bg-slate-950">
        <div className="flex items-center gap-2 p-3">
          <button className="cursor-grab text-slate-400 hover:text-slate-600">
            <GripVertical className="h-4 w-4" />
          </button>
          <div className="flex-1">
            <CollapsibleTrigger className="flex items-center gap-2 w-full text-left">
              <span className="font-medium">Step {index + 1}:</span>
              <span className="text-slate-600 dark:text-slate-400 flex-1 truncate">
                {step.title || 'Untitled Step'}
              </span>
              <div className="flex items-center gap-2 text-xs text-slate-500">
                {step.delayDays ? (
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {step.delayDays}d delay
                  </span>
                ) : null}
                {step.channelType === 'Email' ? (
                  <Mail className="h-3 w-3" />
                ) : step.channelType === 'AppNotification' ? (
                  <Bell className="h-3 w-3" />
                ) : (
                  <>
                    <Mail className="h-3 w-3" />
                    <Bell className="h-3 w-3" />
                  </>
                )}
              </div>
              {isExpanded ? (
                <ChevronUp className="h-4 w-4 text-slate-400" />
              ) : (
                <ChevronDown className="h-4 w-4 text-slate-400" />
              )}
            </CollapsibleTrigger>
          </div>
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveUp}
              disabled={index === 0}
            >
              <ChevronUp className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveDown}
              disabled={index === totalSteps - 1}
            >
              <ChevronDown className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-red-500 hover:text-red-600"
              onClick={onDelete}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <CollapsibleContent>
          <Separator />
          <div className="p-4 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Step Title</Label>
                <Input
                  value={step.title}
                  onChange={(e) => onUpdate({ title: e.target.value })}
                  placeholder="Welcome to the team!"
                />
              </div>
              <div className="space-y-2">
                <Label>Delay (days after previous step)</Label>
                <Input
                  type="number"
                  value={step.delayDays ?? 0}
                  onChange={(e) => onUpdate({ delayDays: parseInt(e.target.value) || 0 })}
                  min={0}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label>Description (optional)</Label>
              <Textarea
                value={step.description || ''}
                onChange={(e) => onUpdate({ description: e.target.value })}
                placeholder="Brief description of this step..."
                rows={2}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Delivery Channel</Label>
                <Select
                  value={step.channelType || 'Both'}
                  onValueChange={(value) => onUpdate({ channelType: value as JourneyChannelType })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(channelTypeLabels).map(([value, label]) => (
                      <SelectItem key={value} value={value}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Required?</Label>
                <div className="flex items-center gap-2 pt-2">
                  <Switch
                    checked={step.isRequired ?? true}
                    onCheckedChange={(checked) => onUpdate({ isRequired: checked })}
                  />
                  <span className="text-sm text-slate-600">
                    {step.isRequired !== false ? 'Required to complete' : 'Optional step'}
                  </span>
                </div>
              </div>
            </div>

            <Separator />

            <div className="space-y-2">
              <Label>Step Content</Label>
              <div className="border rounded-lg p-4 bg-slate-50 dark:bg-slate-900">
                <EmailBlockEditor blocks={blocks} onChange={setBlocks} />
              </div>
            </div>
          </div>
        </CollapsibleContent>
      </div>
    </Collapsible>
  );
}

export function JourneyBuilder({ id }: JourneyBuilderProps) {
  const router = useRouter();
  const { toast } = useToast();
  const isNew = id === 'new';

  const { data: journey, isLoading } = useJourney(id);
  const createMutation = useCreateJourney();
  const updateMutation = useUpdateJourney();
  const updateStepsMutation = useUpdateJourneySteps();
  const activateMutation = useActivateJourney();
  const deactivateMutation = useDeactivateJourney();

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [triggerType, setTriggerType] = useState<JourneyTriggerType>('Manual');
  const [triggerConfig, setTriggerConfig] = useState<TriggerConfig>({});
  const [steps, setSteps] = useState<(JourneyStepInput & { tempId?: string })[]>([]);
  const [expandedStep, setExpandedStep] = useState<number | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Load existing journey data
  useEffect(() => {
    if (journey) {
      setName(journey.name);
      setDescription(journey.description || '');
      setTriggerType(journey.triggerType);
      try {
        setTriggerConfig(JSON.parse(journey.triggerConfig || '{}'));
      } catch {
        setTriggerConfig({});
      }
      setSteps(
        journey.steps.map((s) => ({
          id: s.id,
          title: s.title,
          description: s.description,
          content: s.content,
          delayDays: s.delayDays,
          channelType: s.channelType,
          isRequired: s.isRequired,
        }))
      );
    }
  }, [journey]);

  const currentId = savedId || (isNew ? null : id);

  const handleAddStep = () => {
    const newStep: JourneyStepInput & { tempId: string } = {
      tempId: generateId(),
      title: '',
      description: '',
      content: '[]',
      delayDays: steps.length > 0 ? 1 : 0,
      channelType: 'Both',
      isRequired: true,
    };
    setSteps([...steps, newStep]);
    setExpandedStep(steps.length);
    setHasChanges(true);
  };

  const handleUpdateStep = (index: number, updates: Partial<JourneyStepInput>) => {
    const newSteps = [...steps];
    newSteps[index] = { ...newSteps[index], ...updates };
    setSteps(newSteps);
    setHasChanges(true);
  };

  const handleDeleteStep = (index: number) => {
    setSteps(steps.filter((_, i) => i !== index));
    if (expandedStep === index) {
      setExpandedStep(null);
    } else if (expandedStep !== null && expandedStep > index) {
      setExpandedStep(expandedStep - 1);
    }
    setHasChanges(true);
  };

  const handleMoveStep = (fromIndex: number, toIndex: number) => {
    if (toIndex < 0 || toIndex >= steps.length) return;
    const newSteps = [...steps];
    const [removed] = newSteps.splice(fromIndex, 1);
    newSteps.splice(toIndex, 0, removed);
    setSteps(newSteps);
    setExpandedStep(toIndex);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!name.trim()) {
      toast({
        title: 'Validation Error',
        description: 'Journey name is required',
        variant: 'destructive',
      });
      return;
    }

    try {
      const configJson = JSON.stringify(triggerConfig);

      if (isNew || !currentId) {
        const result = await createMutation.mutateAsync({
          name,
          description: description || undefined,
          triggerType,
          triggerConfig: configJson,
        });
        setSavedId(result.id);

        // Save steps if any
        if (steps.length > 0) {
          await updateStepsMutation.mutateAsync({
            id: result.id,
            steps: steps.map((s) => ({
              id: s.id,
              title: s.title || 'Untitled Step',
              description: s.description,
              content: s.content,
              delayDays: s.delayDays,
              channelType: s.channelType,
              isRequired: s.isRequired,
            })),
          });
        }

        toast({ title: 'Journey created' });
        router.replace(`/studio/journeys/${result.id}`);
      } else {
        await updateMutation.mutateAsync({
          id: currentId,
          name,
          description: description || undefined,
          triggerType,
          triggerConfig: configJson,
        });

        await updateStepsMutation.mutateAsync({
          id: currentId,
          steps: steps.map((s) => ({
            id: s.id,
            title: s.title || 'Untitled Step',
            description: s.description,
            content: s.content,
            delayDays: s.delayDays,
            channelType: s.channelType,
            isRequired: s.isRequired,
          })),
        });

        toast({ title: 'Journey saved' });
      }

      setHasChanges(false);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save journey',
        variant: 'destructive',
      });
    }
  };

  const handleToggleActive = async () => {
    if (!currentId) return;

    try {
      if (journey?.isActive) {
        await deactivateMutation.mutateAsync(currentId);
        toast({ title: 'Journey deactivated' });
      } else {
        await activateMutation.mutateAsync(currentId);
        toast({ title: 'Journey activated' });
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to toggle journey status',
        variant: 'destructive',
      });
    }
  };

  if (isLoading && !isNew) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const isSaving = createMutation.isPending || updateMutation.isPending || updateStepsMutation.isPending;
  const isToggling = activateMutation.isPending || deactivateMutation.isPending;

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push('/studio/journeys')}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">
                {isNew ? 'New Journey' : 'Edit Journey'}
              </h1>
              {hasChanges && <p className="text-xs text-muted-foreground">Unsaved changes</p>}
            </div>
            {journey && (
              <Badge variant={journey.isActive ? 'default' : 'secondary'}>
                {journey.isActive ? 'Active' : 'Inactive'}
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button onClick={handleSave} disabled={isSaving}>
              {isSaving ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="h-4 w-4 mr-2" />
                  Save
                </>
              )}
            </Button>
            {currentId && (
              <Button
                variant={journey?.isActive ? 'outline' : 'default'}
                onClick={handleToggleActive}
                disabled={isToggling || steps.length === 0}
              >
                {isToggling ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : journey?.isActive ? (
                  <>
                    <Pause className="h-4 w-4 mr-2" />
                    Deactivate
                  </>
                ) : (
                  <>
                    <Play className="h-4 w-4 mr-2" />
                    Activate
                  </>
                )}
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex overflow-hidden">
        {/* Steps Editor */}
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-3xl mx-auto space-y-4">
            <h2 className="font-semibold mb-4">Journey Steps</h2>

            {steps.length === 0 ? (
              <div className="text-center py-12 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-lg">
                <p className="text-slate-500 mb-4">No steps yet. Add your first step to get started.</p>
                <Button onClick={handleAddStep}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add First Step
                </Button>
              </div>
            ) : (
              <div className="space-y-3">
                {steps.map((step, index) => (
                  <div key={step.id || step.tempId} className="group">
                    <StepEditor
                      step={step}
                      index={index}
                      totalSteps={steps.length}
                      isExpanded={expandedStep === index}
                      onToggle={() => setExpandedStep(expandedStep === index ? null : index)}
                      onUpdate={(updates) => handleUpdateStep(index, updates)}
                      onDelete={() => handleDeleteStep(index)}
                      onMoveUp={() => handleMoveStep(index, index - 1)}
                      onMoveDown={() => handleMoveStep(index, index + 1)}
                    />
                  </div>
                ))}
                <Button variant="outline" className="w-full" onClick={handleAddStep}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Step
                </Button>
              </div>
            )}
          </div>
        </div>

        {/* Settings Sidebar */}
        <div className="w-80 border-l bg-muted/30 overflow-auto p-6 space-y-6">
          <div className="space-y-4">
            <h2 className="font-semibold">Journey Settings</h2>

            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => {
                  setName(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="New Employee Onboarding"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => {
                  setDescription(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="Welcome new team members..."
                rows={3}
              />
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Trigger</h2>

            <div className="space-y-2">
              <Label>Trigger Type</Label>
              <Select
                value={triggerType}
                onValueChange={(value) => {
                  setTriggerType(value as JourneyTriggerType);
                  setTriggerConfig({});
                  setHasChanges(true);
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(triggerTypeLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {triggerType === 'DateBased' && (
              <>
                <div className="space-y-2">
                  <Label>Date Field</Label>
                  <Select
                    value={triggerConfig.dateField || ''}
                    onValueChange={(value) => {
                      setTriggerConfig({ ...triggerConfig, dateField: value });
                      setHasChanges(true);
                    }}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select field" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="hireDate">Hire Date (Anniversary)</SelectItem>
                      <SelectItem value="birthday">Birthday</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Days Offset</Label>
                  <Input
                    type="number"
                    value={triggerConfig.daysOffset ?? 0}
                    onChange={(e) => {
                      setTriggerConfig({ ...triggerConfig, daysOffset: parseInt(e.target.value) || 0 });
                      setHasChanges(true);
                    }}
                    placeholder="0 = on the date, -7 = 7 days before"
                  />
                  <p className="text-xs text-muted-foreground">
                    Negative = before, Positive = after
                  </p>
                </div>
              </>
            )}

            {triggerType === 'RoleChange' && (
              <div className="space-y-2">
                <Label>Target Roles (one per line)</Label>
                <Textarea
                  value={triggerConfig.targetRoles?.join('\n') || ''}
                  onChange={(e) => {
                    setTriggerConfig({
                      ...triggerConfig,
                      targetRoles: e.target.value.split('\n').filter(Boolean),
                    });
                    setHasChanges(true);
                  }}
                  placeholder="Manager&#10;Director&#10;VP"
                  rows={3}
                />
              </div>
            )}

            {triggerType === 'LocationChange' && (
              <div className="space-y-2">
                <Label>Target Locations (one per line)</Label>
                <Textarea
                  value={triggerConfig.targetLocations?.join('\n') || ''}
                  onChange={(e) => {
                    setTriggerConfig({
                      ...triggerConfig,
                      targetLocations: e.target.value.split('\n').filter(Boolean),
                    });
                    setHasChanges(true);
                  }}
                  placeholder="New York&#10;London&#10;Tokyo"
                  rows={3}
                />
              </div>
            )}
          </div>

          {currentId && (
            <>
              <Separator />
              <div className="space-y-2">
                <h2 className="font-semibold">Stats</h2>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-muted-foreground">Enrollments</p>
                    <p className="font-medium">{journey?.enrollmentCount || 0}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Active</p>
                    <p className="font-medium">{journey?.activeEnrollments || 0}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Completed</p>
                    <p className="font-medium">{journey?.completedEnrollments || 0}</p>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
