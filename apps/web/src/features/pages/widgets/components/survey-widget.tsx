'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2, CheckCircle2, AlertCircle, ClipboardList } from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import type { WidgetProps, SurveyData } from '../types';

interface SurveyQuestion {
  id: string;
  type: 'SingleChoice' | 'MultiChoice' | 'FreeText' | 'Rating' | 'NPS' | 'YesNo';
  questionText: string;
  description?: string;
  options?: string; // JSON array
  isRequired: boolean;
  sortOrder: number;
  settings?: string;
}

interface SurveyDetail {
  id: string;
  title: string;
  description?: string;
  isAnonymous: boolean;
  status: string;
  questions: SurveyQuestion[];
}

interface MyResponse {
  hasResponse: boolean;
  isComplete: boolean;
  answers?: Record<string, unknown>;
}

async function fetchSurvey(surveyId: string): Promise<SurveyDetail> {
  return apiClient.get<SurveyDetail>(`/surveys/${surveyId}`);
}

async function fetchMyResponse(surveyId: string): Promise<MyResponse> {
  return apiClient.get<MyResponse>(`/surveys/${surveyId}/my-response`);
}

async function submitResponse(
  surveyId: string,
  answers: Record<string, unknown>
): Promise<void> {
  await apiClient.post(`/surveys/${surveyId}/respond`, { answers });
}

function parseOptions(optionsJson?: string): string[] {
  if (!optionsJson) return [];
  try {
    return JSON.parse(optionsJson);
  } catch {
    return [];
  }
}

export default function SurveyWidget({ data, id }: WidgetProps<SurveyData>) {
  const queryClient = useQueryClient();
  const [currentStep, setCurrentStep] = useState(0);
  const [answers, setAnswers] = useState<Record<string, unknown>>({});
  const [error, setError] = useState<string | null>(null);

  const showResults = data.showResults !== false;
  const showProgress = data.showProgress !== false;

  const { data: survey, isLoading: surveyLoading } = useQuery({
    queryKey: ['widget-survey', data.surveyId],
    queryFn: () => fetchSurvey(data.surveyId),
    enabled: !!data.surveyId,
  });

  const { data: myResponse, isLoading: responseLoading } = useQuery({
    queryKey: ['widget-survey-response', data.surveyId],
    queryFn: () => fetchMyResponse(data.surveyId),
    enabled: !!data.surveyId,
  });

  const submitMutation = useMutation({
    mutationFn: () => submitResponse(data.surveyId, answers),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['widget-survey-response', data.surveyId] });
    },
    onError: (err: Error) => {
      setError(err.message || 'Failed to submit response');
    },
  });

  const isLoading = surveyLoading || responseLoading;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!survey) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6 text-center">
        <AlertCircle className="h-8 w-8 text-slate-300 dark:text-slate-600 mx-auto mb-2" />
        <p className="text-sm text-slate-500">Survey not found</p>
      </div>
    );
  }

  if (survey.status !== 'Active') {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6 text-center">
        <ClipboardList className="h-8 w-8 text-slate-300 dark:text-slate-600 mx-auto mb-2" />
        <p className="text-sm text-slate-500">This survey is not currently active</p>
      </div>
    );
  }

  // Already completed
  if (myResponse?.hasResponse && myResponse.isComplete) {
    return (
      <div className="rounded-lg border border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-950 p-6 text-center">
        <CheckCircle2 className="h-8 w-8 text-green-500 mx-auto mb-2" />
        <p className="font-medium text-green-800 dark:text-green-200">Thank you!</p>
        <p className="text-sm text-green-600 dark:text-green-400 mt-1">
          You have already completed this survey
        </p>
        {showResults && (
          <p className="text-xs text-green-500 mt-2">
            Results will be shared when the survey closes
          </p>
        )}
      </div>
    );
  }

  const questions = survey.questions.sort((a, b) => a.sortOrder - b.sortOrder);
  const currentQuestion = questions[currentStep];
  const progress = ((currentStep + 1) / questions.length) * 100;

  const setAnswer = (questionId: string, value: unknown) => {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
    setError(null);
  };

  const handleNext = () => {
    if (currentQuestion.isRequired && !answers[currentQuestion.id]) {
      setError('This question is required');
      return;
    }
    setError(null);
    if (currentStep < questions.length - 1) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handlePrevious = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleSubmit = () => {
    if (currentQuestion.isRequired && !answers[currentQuestion.id]) {
      setError('This question is required');
      return;
    }
    submitMutation.mutate();
  };

  const isLastQuestion = currentStep === questions.length - 1;

  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6">
      <h3 className="font-semibold text-lg mb-2">{survey.title}</h3>
      {survey.description && (
        <p className="text-sm text-slate-600 dark:text-slate-400 mb-4">{survey.description}</p>
      )}

      {showProgress && questions.length > 1 && (
        <div className="mb-6">
          <div className="flex justify-between text-xs text-slate-500 mb-1">
            <span>Question {currentStep + 1} of {questions.length}</span>
            <span>{Math.round(progress)}%</span>
          </div>
          <Progress value={progress} className="h-2" />
        </div>
      )}

      <div className="min-h-[200px]">
        <QuestionInput
          question={currentQuestion}
          value={answers[currentQuestion.id]}
          onChange={(value) => setAnswer(currentQuestion.id, value)}
        />
      </div>

      {error && (
        <p className="text-sm text-red-500 mt-4">{error}</p>
      )}

      <div className="flex justify-between mt-6">
        <Button
          variant="outline"
          onClick={handlePrevious}
          disabled={currentStep === 0 || submitMutation.isPending}
        >
          Previous
        </Button>

        {isLastQuestion ? (
          <Button
            onClick={handleSubmit}
            disabled={submitMutation.isPending}
          >
            {submitMutation.isPending ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Submitting...
              </>
            ) : (
              'Submit'
            )}
          </Button>
        ) : (
          <Button onClick={handleNext}>
            Next
          </Button>
        )}
      </div>
    </div>
  );
}

interface QuestionInputProps {
  question: SurveyQuestion;
  value: unknown;
  onChange: (value: unknown) => void;
}

function QuestionInput({ question, value, onChange }: QuestionInputProps) {
  const options = parseOptions(question.options);

  return (
    <div className="space-y-4">
      <div>
        <Label className="text-base font-medium">
          {question.questionText}
          {question.isRequired && <span className="text-red-500 ml-1">*</span>}
        </Label>
        {question.description && (
          <p className="text-sm text-slate-500 mt-1">{question.description}</p>
        )}
      </div>

      {question.type === 'SingleChoice' && (
        <RadioGroup
          value={value as string || ''}
          onValueChange={onChange}
        >
          {options.map((option, idx) => (
            <div key={idx} className="flex items-center space-x-2">
              <RadioGroupItem value={option} id={`${question.id}-${idx}`} />
              <Label htmlFor={`${question.id}-${idx}`} className="font-normal">
                {option}
              </Label>
            </div>
          ))}
        </RadioGroup>
      )}

      {question.type === 'MultiChoice' && (
        <div className="space-y-2">
          {options.map((option, idx) => {
            const selected = Array.isArray(value) ? value.includes(option) : false;
            return (
              <div key={idx} className="flex items-center space-x-2">
                <Checkbox
                  id={`${question.id}-${idx}`}
                  checked={selected}
                  onCheckedChange={(checked) => {
                    const current = Array.isArray(value) ? value : [];
                    if (checked) {
                      onChange([...current, option]);
                    } else {
                      onChange(current.filter((v: string) => v !== option));
                    }
                  }}
                />
                <Label htmlFor={`${question.id}-${idx}`} className="font-normal">
                  {option}
                </Label>
              </div>
            );
          })}
        </div>
      )}

      {question.type === 'YesNo' && (
        <RadioGroup
          value={value as string || ''}
          onValueChange={onChange}
        >
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="yes" id={`${question.id}-yes`} />
            <Label htmlFor={`${question.id}-yes`} className="font-normal">Yes</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="no" id={`${question.id}-no`} />
            <Label htmlFor={`${question.id}-no`} className="font-normal">No</Label>
          </div>
        </RadioGroup>
      )}

      {question.type === 'FreeText' && (
        <Textarea
          value={value as string || ''}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Type your answer here..."
          rows={4}
        />
      )}

      {question.type === 'Rating' && (
        <RatingInput
          value={value as number | undefined}
          onChange={onChange}
          max={5}
        />
      )}

      {question.type === 'NPS' && (
        <NPSInput
          value={value as number | undefined}
          onChange={onChange}
        />
      )}
    </div>
  );
}

interface RatingInputProps {
  value?: number;
  onChange: (value: number) => void;
  max?: number;
}

function RatingInput({ value, onChange, max = 5 }: RatingInputProps) {
  return (
    <div className="flex gap-2">
      {Array.from({ length: max }, (_, i) => i + 1).map((rating) => (
        <button
          key={rating}
          type="button"
          onClick={() => onChange(rating)}
          className={cn(
            'w-10 h-10 rounded-full border-2 text-sm font-medium transition-colors',
            value === rating
              ? 'border-blue-500 bg-blue-500 text-white'
              : 'border-slate-300 dark:border-slate-600 hover:border-blue-400'
          )}
        >
          {rating}
        </button>
      ))}
    </div>
  );
}

interface NPSInputProps {
  value?: number;
  onChange: (value: number) => void;
}

function NPSInput({ value, onChange }: NPSInputProps) {
  return (
    <div className="space-y-2">
      <div className="flex gap-1">
        {Array.from({ length: 11 }, (_, i) => i).map((rating) => (
          <button
            key={rating}
            type="button"
            onClick={() => onChange(rating)}
            className={cn(
              'w-9 h-9 rounded text-sm font-medium transition-colors',
              value === rating
                ? 'bg-blue-500 text-white'
                : rating <= 6
                  ? 'bg-red-100 dark:bg-red-900/30 hover:bg-red-200'
                  : rating <= 8
                    ? 'bg-yellow-100 dark:bg-yellow-900/30 hover:bg-yellow-200'
                    : 'bg-green-100 dark:bg-green-900/30 hover:bg-green-200'
            )}
          >
            {rating}
          </button>
        ))}
      </div>
      <div className="flex justify-between text-xs text-slate-500">
        <span>Not at all likely</span>
        <span>Extremely likely</span>
      </div>
    </div>
  );
}
