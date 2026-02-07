'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import {
  Save,
  Play,
  Pause,
  ChevronLeft,
  Plus,
  GripVertical,
  Trash2,
  ChevronDown,
  ChevronUp,
  Loader2,
  ListChecks,
  MessageSquare,
  Star,
  ThumbsUp,
  ToggleLeft,
  Hash,
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
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { Separator } from '@/components/ui/separator';
import { useToast } from '@/hooks/use-toast';
import {
  useSurvey,
  useCreateSurvey,
  useUpdateSurvey,
  useUpdateSurveyQuestions,
  useActivateSurvey,
  useCloseSurvey,
} from '../api';
import type { SurveyQuestionType, SurveyQuestionInput } from '../types';

interface SurveyBuilderProps {
  id: string;
}

const questionTypeConfig: Record<SurveyQuestionType, { label: string; icon: React.ComponentType<{ className?: string }>; description: string }> = {
  SingleChoice: { label: 'Single Choice', icon: ListChecks, description: 'Select one option' },
  MultiChoice: { label: 'Multiple Choice', icon: ListChecks, description: 'Select multiple options' },
  FreeText: { label: 'Free Text', icon: MessageSquare, description: 'Open-ended response' },
  Rating: { label: 'Rating', icon: Star, description: '1-5 star rating' },
  NPS: { label: 'NPS', icon: Hash, description: '0-10 recommendation score' },
  YesNo: { label: 'Yes/No', icon: ToggleLeft, description: 'Binary choice' },
};

function generateId(): string {
  return `q-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

function parseOptions(optionsJson?: string): string[] {
  if (!optionsJson) return [];
  try {
    return JSON.parse(optionsJson);
  } catch {
    return [];
  }
}

interface QuestionEditorProps {
  question: SurveyQuestionInput;
  index: number;
  totalQuestions: number;
  isExpanded: boolean;
  onToggle: () => void;
  onUpdate: (updates: Partial<SurveyQuestionInput>) => void;
  onDelete: () => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
}

function QuestionEditor({
  question,
  index,
  totalQuestions,
  isExpanded,
  onToggle,
  onUpdate,
  onDelete,
  onMoveUp,
  onMoveDown,
}: QuestionEditorProps) {
  const [options, setOptions] = useState<string[]>(() => parseOptions(question.options));
  const TypeIcon = questionTypeConfig[question.type]?.icon || ListChecks;

  useEffect(() => {
    if (['SingleChoice', 'MultiChoice'].includes(question.type)) {
      onUpdate({ options: JSON.stringify(options.filter(Boolean)) });
    }
  }, [options, question.type]);

  const handleAddOption = () => {
    setOptions([...options, '']);
  };

  const handleUpdateOption = (idx: number, value: string) => {
    const newOptions = [...options];
    newOptions[idx] = value;
    setOptions(newOptions);
  };

  const handleRemoveOption = (idx: number) => {
    setOptions(options.filter((_, i) => i !== idx));
  };

  return (
    <Collapsible open={isExpanded} onOpenChange={onToggle}>
      <div className="border rounded-lg bg-white dark:bg-slate-950">
        <div className="flex items-center gap-2 p-3 group">
          <button className="cursor-grab text-slate-400 hover:text-slate-600">
            <GripVertical className="h-4 w-4" />
          </button>
          <div className="flex-1">
            <CollapsibleTrigger className="flex items-center gap-2 w-full text-left">
              <TypeIcon className="h-4 w-4 text-muted-foreground" />
              <span className="font-medium">Q{index + 1}:</span>
              <span className="text-slate-600 dark:text-slate-400 flex-1 truncate">
                {question.questionText || 'Untitled Question'}
              </span>
              {question.isRequired && (
                <span className="text-xs text-red-500">Required</span>
              )}
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
              disabled={index === totalQuestions - 1}
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
                <Label>Question Type</Label>
                <Select
                  value={question.type}
                  onValueChange={(value) => onUpdate({ type: value as SurveyQuestionType })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(questionTypeConfig).map(([type, config]) => (
                      <SelectItem key={type} value={type}>
                        <div className="flex items-center gap-2">
                          <config.icon className="h-4 w-4" />
                          {config.label}
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Required?</Label>
                <div className="flex items-center gap-2 pt-2">
                  <Switch
                    checked={question.isRequired !== false}
                    onCheckedChange={(checked) => onUpdate({ isRequired: checked })}
                  />
                  <span className="text-sm text-slate-600">
                    {question.isRequired !== false ? 'Required' : 'Optional'}
                  </span>
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Question Text</Label>
              <Input
                value={question.questionText}
                onChange={(e) => onUpdate({ questionText: e.target.value })}
                placeholder="Enter your question..."
              />
            </div>

            <div className="space-y-2">
              <Label>Description (optional)</Label>
              <Textarea
                value={question.description || ''}
                onChange={(e) => onUpdate({ description: e.target.value })}
                placeholder="Additional context for the question..."
                rows={2}
              />
            </div>

            {['SingleChoice', 'MultiChoice'].includes(question.type) && (
              <div className="space-y-2">
                <Label>Options</Label>
                <div className="space-y-2">
                  {options.map((option, idx) => (
                    <div key={idx} className="flex items-center gap-2">
                      <Input
                        value={option}
                        onChange={(e) => handleUpdateOption(idx, e.target.value)}
                        placeholder={`Option ${idx + 1}`}
                      />
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-red-500"
                        onClick={() => handleRemoveOption(idx)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                  <Button variant="outline" size="sm" onClick={handleAddOption}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Option
                  </Button>
                </div>
              </div>
            )}
          </div>
        </CollapsibleContent>
      </div>
    </Collapsible>
  );
}

export function SurveyBuilder({ id }: SurveyBuilderProps) {
  const router = useRouter();
  const { toast } = useToast();
  const isNew = id === 'new';

  const { data: survey, isLoading } = useSurvey(id);
  const createMutation = useCreateSurvey();
  const updateMutation = useUpdateSurvey();
  const updateQuestionsMutation = useUpdateSurveyQuestions();
  const activateMutation = useActivateSurvey();
  const closeMutation = useCloseSurvey();

  // Form state
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [questions, setQuestions] = useState<SurveyQuestionInput[]>([]);
  const [expandedQuestion, setExpandedQuestion] = useState<number | null>(null);
  const [savedId, setSavedId] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Load existing survey data
  useEffect(() => {
    if (survey) {
      setTitle(survey.title);
      setDescription(survey.description || '');
      setIsAnonymous(survey.isAnonymous);
      setQuestions(
        survey.questions.map((q) => ({
          id: q.id,
          type: q.type,
          questionText: q.questionText,
          description: q.description,
          options: q.options,
          isRequired: q.isRequired,
          settings: q.settings,
        }))
      );
    }
  }, [survey]);

  const currentId = savedId || (isNew ? null : id);

  const handleAddQuestion = (type: SurveyQuestionType = 'SingleChoice') => {
    const newQuestion: SurveyQuestionInput = {
      tempId: generateId(),
      type,
      questionText: '',
      description: '',
      options: type === 'SingleChoice' || type === 'MultiChoice' ? '[]' : undefined,
      isRequired: true,
    };
    setQuestions([...questions, newQuestion]);
    setExpandedQuestion(questions.length);
    setHasChanges(true);
  };

  const handleUpdateQuestion = (index: number, updates: Partial<SurveyQuestionInput>) => {
    const newQuestions = [...questions];
    newQuestions[index] = { ...newQuestions[index], ...updates };
    setQuestions(newQuestions);
    setHasChanges(true);
  };

  const handleDeleteQuestion = (index: number) => {
    setQuestions(questions.filter((_, i) => i !== index));
    if (expandedQuestion === index) {
      setExpandedQuestion(null);
    } else if (expandedQuestion !== null && expandedQuestion > index) {
      setExpandedQuestion(expandedQuestion - 1);
    }
    setHasChanges(true);
  };

  const handleMoveQuestion = (fromIndex: number, toIndex: number) => {
    if (toIndex < 0 || toIndex >= questions.length) return;
    const newQuestions = [...questions];
    const [removed] = newQuestions.splice(fromIndex, 1);
    newQuestions.splice(toIndex, 0, removed);
    setQuestions(newQuestions);
    setExpandedQuestion(toIndex);
    setHasChanges(true);
  };

  const handleSave = async () => {
    if (!title.trim()) {
      toast({
        title: 'Validation Error',
        description: 'Survey title is required',
        variant: 'destructive',
      });
      return;
    }

    try {
      if (isNew || !currentId) {
        const result = await createMutation.mutateAsync({
          title,
          description: description || undefined,
          isAnonymous,
        });
        setSavedId(result.id);

        // Save questions if any
        if (questions.length > 0) {
          await updateQuestionsMutation.mutateAsync({
            id: result.id,
            questions: questions.map((q) => ({
              id: q.id,
              type: q.type,
              questionText: q.questionText || 'Untitled Question',
              description: q.description,
              options: q.options,
              isRequired: q.isRequired,
              settings: q.settings,
            })),
          });
        }

        toast({ title: 'Survey created' });
        router.replace(`/studio/surveys/${result.id}`);
      } else {
        await updateMutation.mutateAsync({
          id: currentId,
          title,
          description: description || undefined,
          isAnonymous,
        });

        await updateQuestionsMutation.mutateAsync({
          id: currentId,
          questions: questions.map((q) => ({
            id: q.id,
            type: q.type,
            questionText: q.questionText || 'Untitled Question',
            description: q.description,
            options: q.options,
            isRequired: q.isRequired,
            settings: q.settings,
          })),
        });

        toast({ title: 'Survey saved' });
      }

      setHasChanges(false);
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to save survey',
        variant: 'destructive',
      });
    }
  };

  const handleToggleStatus = async () => {
    if (!currentId) return;

    try {
      if (survey?.status === 'Active') {
        await closeMutation.mutateAsync(currentId);
        toast({ title: 'Survey closed' });
      } else {
        await activateMutation.mutateAsync(currentId);
        toast({ title: 'Survey activated' });
      }
    } catch (error) {
      toast({
        title: 'Error',
        description: 'Failed to update survey status',
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

  const isSaving = createMutation.isPending || updateMutation.isPending || updateQuestionsMutation.isPending;
  const isToggling = activateMutation.isPending || closeMutation.isPending;

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push('/studio/surveys')}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">
                {isNew ? 'New Survey' : 'Edit Survey'}
              </h1>
              {hasChanges && <p className="text-xs text-muted-foreground">Unsaved changes</p>}
            </div>
            {survey && (
              <Badge variant={survey.status === 'Active' ? 'default' : 'secondary'}>
                {survey.status}
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
            {currentId && survey?.status !== 'Closed' && (
              <Button
                variant={survey?.status === 'Active' ? 'outline' : 'default'}
                onClick={handleToggleStatus}
                disabled={isToggling || questions.length === 0}
              >
                {isToggling ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : survey?.status === 'Active' ? (
                  <>
                    <Pause className="h-4 w-4 mr-2" />
                    Close
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
        {/* Questions Editor */}
        <div className="flex-1 overflow-auto p-6">
          <div className="max-w-3xl mx-auto space-y-4">
            <h2 className="font-semibold mb-4">Questions</h2>

            {questions.length === 0 ? (
              <div className="text-center py-12 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-lg">
                <p className="text-slate-500 mb-4">No questions yet. Add your first question to get started.</p>
                <div className="flex flex-wrap gap-2 justify-center">
                  {Object.entries(questionTypeConfig).map(([type, config]) => (
                    <Button
                      key={type}
                      variant="outline"
                      size="sm"
                      onClick={() => handleAddQuestion(type as SurveyQuestionType)}
                    >
                      <config.icon className="h-4 w-4 mr-2" />
                      {config.label}
                    </Button>
                  ))}
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                {questions.map((question, index) => (
                  <div key={question.id || question.tempId} className="group">
                    <QuestionEditor
                      question={question}
                      index={index}
                      totalQuestions={questions.length}
                      isExpanded={expandedQuestion === index}
                      onToggle={() => setExpandedQuestion(expandedQuestion === index ? null : index)}
                      onUpdate={(updates) => handleUpdateQuestion(index, updates)}
                      onDelete={() => handleDeleteQuestion(index)}
                      onMoveUp={() => handleMoveQuestion(index, index - 1)}
                      onMoveDown={() => handleMoveQuestion(index, index + 1)}
                    />
                  </div>
                ))}
                <div className="flex flex-wrap gap-2">
                  {Object.entries(questionTypeConfig).map(([type, config]) => (
                    <Button
                      key={type}
                      variant="outline"
                      size="sm"
                      onClick={() => handleAddQuestion(type as SurveyQuestionType)}
                    >
                      <config.icon className="h-4 w-4 mr-2" />
                      Add {config.label}
                    </Button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Settings Sidebar */}
        <div className="w-80 border-l bg-muted/30 overflow-auto p-6 space-y-6">
          <div className="space-y-4">
            <h2 className="font-semibold">Survey Settings</h2>

            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => {
                  setTitle(e.target.value);
                  setHasChanges(true);
                }}
                placeholder="Employee Satisfaction Survey"
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
                placeholder="Help us understand how we can improve..."
                rows={3}
              />
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <h2 className="font-semibold">Privacy</h2>

            <div className="flex items-center justify-between">
              <div>
                <Label>Anonymous Responses</Label>
                <p className="text-xs text-muted-foreground">
                  Respondent identities will be hidden
                </p>
              </div>
              <Switch
                checked={isAnonymous}
                onCheckedChange={(checked) => {
                  setIsAnonymous(checked);
                  setHasChanges(true);
                }}
                disabled={survey?.status === 'Active'}
              />
            </div>
          </div>

          {currentId && (
            <>
              <Separator />
              <div className="space-y-2">
                <h2 className="font-semibold">Stats</h2>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-muted-foreground">Questions</p>
                    <p className="font-medium">{questions.length}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Responses</p>
                    <p className="font-medium">{survey?.responseCount || 0}</p>
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
