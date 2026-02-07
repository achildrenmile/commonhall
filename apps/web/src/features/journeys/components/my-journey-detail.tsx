'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { format } from 'date-fns';
import {
  ChevronLeft,
  CheckCircle2,
  Circle,
  Clock,
  PlayCircle,
  PauseCircle,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { useToast } from '@/hooks/use-toast';
import { PageViewer } from '@/features/pages/components';
import {
  useMyJourneyDetail,
  useCompleteStep,
  useMarkStepViewed,
  usePauseMyJourney,
  useResumeMyJourney,
} from '../api';
import type { MyJourneyStep } from '../types';

interface MyJourneyDetailProps {
  enrollmentId: string;
}

function StepCard({
  step,
  enrollmentId,
  onComplete,
  isCompleting,
}: {
  step: MyJourneyStep;
  enrollmentId: string;
  onComplete: () => void;
  isCompleting: boolean;
}) {
  const viewMutation = useMarkStepViewed();
  const widgets = JSON.parse(step.content || '[]');

  useEffect(() => {
    if (step.isDelivered && !step.viewedAt) {
      viewMutation.mutate({ enrollmentId, stepIndex: step.stepIndex });
    }
  }, [step.isDelivered, step.viewedAt, enrollmentId, step.stepIndex]);

  return (
    <Card className={step.isCurrentStep ? 'border-primary' : ''}>
      <CardHeader>
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-3">
            <div className="mt-1">
              {step.isCompleted ? (
                <CheckCircle2 className="h-5 w-5 text-green-500" />
              ) : step.isDelivered ? (
                <Circle className="h-5 w-5 text-primary" />
              ) : (
                <Clock className="h-5 w-5 text-muted-foreground" />
              )}
            </div>
            <div>
              <CardTitle className="text-lg">
                Step {step.stepIndex + 1}: {step.title}
              </CardTitle>
              {step.description && (
                <CardDescription className="mt-1">{step.description}</CardDescription>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2">
            {step.isRequired && (
              <Badge variant="outline" className="text-xs">
                Required
              </Badge>
            )}
            {step.isCompleted && (
              <Badge variant="default" className="bg-green-500">
                Completed
              </Badge>
            )}
            {step.isCurrentStep && !step.isCompleted && step.isDelivered && (
              <Badge>Current</Badge>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {step.isDelivered ? (
          <div className="space-y-4">
            {widgets.length > 0 && (
              <div className="border rounded-lg p-4 bg-slate-50 dark:bg-slate-900">
                <PageViewer widgets={widgets} />
              </div>
            )}

            {!step.isCompleted && step.isRequired && (
              <Button onClick={onComplete} disabled={isCompleting} className="w-full">
                {isCompleting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Completing...
                  </>
                ) : (
                  <>
                    <CheckCircle2 className="h-4 w-4 mr-2" />
                    Mark as Complete
                  </>
                )}
              </Button>
            )}

            <div className="flex items-center gap-4 text-xs text-muted-foreground">
              {step.deliveredAt && (
                <span>Delivered: {format(new Date(step.deliveredAt), 'MMM d, yyyy h:mm a')}</span>
              )}
              {step.completedAt && (
                <span>Completed: {format(new Date(step.completedAt), 'MMM d, yyyy h:mm a')}</span>
              )}
            </div>
          </div>
        ) : (
          <div className="py-8 text-center text-muted-foreground">
            <Clock className="h-8 w-8 mx-auto mb-2" />
            <p>This step will be available in {step.delayDays} days</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export function MyJourneyDetail({ enrollmentId }: MyJourneyDetailProps) {
  const router = useRouter();
  const { toast } = useToast();
  const { data: journey, isLoading } = useMyJourneyDetail(enrollmentId);
  const completeMutation = useCompleteStep();
  const pauseMutation = usePauseMyJourney();
  const resumeMutation = useResumeMyJourney();

  const handleCompleteStep = async (stepIndex: number) => {
    try {
      await completeMutation.mutateAsync({ enrollmentId, stepIndex });
      toast({ title: 'Step completed!' });
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to complete step',
        variant: 'destructive',
      });
    }
  };

  const handlePause = async () => {
    try {
      await pauseMutation.mutateAsync(enrollmentId);
      toast({ title: 'Journey paused' });
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to pause journey',
        variant: 'destructive',
      });
    }
  };

  const handleResume = async () => {
    try {
      await resumeMutation.mutateAsync(enrollmentId);
      toast({ title: 'Journey resumed' });
    } catch {
      toast({
        title: 'Error',
        description: 'Failed to resume journey',
        variant: 'destructive',
      });
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6 p-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div>
            <Skeleton className="h-6 w-48 mb-2" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <Card>
          <CardContent className="py-6">
            <div className="space-y-3">
              <div className="flex justify-between">
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-4 w-24" />
              </div>
              <Skeleton className="h-2 w-full" />
            </div>
          </CardContent>
        </Card>
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Card key={i}>
              <CardHeader>
                <Skeleton className="h-5 w-48" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-32 w-full" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (!journey) {
    return (
      <div className="flex flex-col items-center justify-center h-96 gap-4">
        <AlertCircle className="h-12 w-12 text-muted-foreground" />
        <p className="text-muted-foreground">Journey not found</p>
        <Button variant="outline" onClick={() => router.push('/journeys')}>
          Back to My Journeys
        </Button>
      </div>
    );
  }

  const isPausingOrResuming = pauseMutation.isPending || resumeMutation.isPending;

  return (
    <div className="space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => router.push('/journeys')}>
            <ChevronLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">{journey.journeyName}</h1>
            {journey.journeyDescription && (
              <p className="text-muted-foreground">{journey.journeyDescription}</p>
            )}
          </div>
        </div>
        {journey.status === 'Active' && (
          <Button variant="outline" onClick={handlePause} disabled={isPausingOrResuming}>
            {isPausingOrResuming ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <PauseCircle className="h-4 w-4 mr-2" />
            )}
            Pause Journey
          </Button>
        )}
        {journey.status === 'Paused' && (
          <Button onClick={handleResume} disabled={isPausingOrResuming}>
            {isPausingOrResuming ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <PlayCircle className="h-4 w-4 mr-2" />
            )}
            Resume Journey
          </Button>
        )}
      </div>

      {/* Progress Card */}
      <Card>
        <CardContent className="py-6">
          <div className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Your Progress</span>
              <span className="font-medium">
                {journey.completedSteps} of {journey.totalSteps} steps completed ({journey.progressPercent}%)
              </span>
            </div>
            <Progress value={journey.progressPercent} className="h-3" />
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Started {format(new Date(journey.startedAt), 'MMM d, yyyy')}</span>
              {journey.completedAt && (
                <span>Completed {format(new Date(journey.completedAt), 'MMM d, yyyy')}</span>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Steps */}
      <div className="space-y-4">
        <h2 className="font-semibold">Journey Steps</h2>
        {journey.steps.map((step) => (
          <StepCard
            key={step.stepIndex}
            step={step}
            enrollmentId={enrollmentId}
            onComplete={() => handleCompleteStep(step.stepIndex)}
            isCompleting={completeMutation.isPending}
          />
        ))}
      </div>
    </div>
  );
}
