'use client';

import Link from 'next/link';
import { format } from 'date-fns';
import {
  Route,
  CheckCircle2,
  PlayCircle,
  PauseCircle,
  XCircle,
  ChevronRight,
  Loader2,
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { useMyJourneys } from '../api';
import type { JourneyEnrollmentStatus, MyJourney } from '../types';

const statusConfig: Record<JourneyEnrollmentStatus, { label: string; icon: React.ElementType; color: string }> = {
  Active: { label: 'In Progress', icon: PlayCircle, color: 'text-blue-500' },
  Paused: { label: 'Paused', icon: PauseCircle, color: 'text-yellow-500' },
  Completed: { label: 'Completed', icon: CheckCircle2, color: 'text-green-500' },
  Cancelled: { label: 'Cancelled', icon: XCircle, color: 'text-slate-500' },
};

function JourneyCard({ journey }: { journey: MyJourney }) {
  const config = statusConfig[journey.status];
  const Icon = config.icon;

  return (
    <Link href={`/journeys/${journey.enrollmentId}`}>
      <Card className="hover:shadow-md transition-shadow cursor-pointer">
        <CardHeader className="pb-2">
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className="text-lg">{journey.journeyName}</CardTitle>
              {journey.journeyDescription && (
                <CardDescription className="mt-1 line-clamp-2">
                  {journey.journeyDescription}
                </CardDescription>
              )}
            </div>
            <Badge variant={journey.status === 'Completed' ? 'default' : 'secondary'} className="gap-1">
              <Icon className={`h-3 w-3 ${config.color}`} />
              {config.label}
            </Badge>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div>
              <div className="flex justify-between text-sm mb-1">
                <span className="text-muted-foreground">Progress</span>
                <span className="font-medium">
                  {journey.completedSteps} / {journey.totalSteps} steps
                </span>
              </div>
              <Progress value={journey.progressPercent} className="h-2" />
            </div>

            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">
                Started {format(new Date(journey.startedAt), 'MMM d, yyyy')}
              </span>
              {journey.status === 'Active' && (
                <span className="flex items-center gap-1 text-primary">
                  Continue
                  <ChevronRight className="h-4 w-4" />
                </span>
              )}
              {journey.completedAt && (
                <span className="text-muted-foreground">
                  Completed {format(new Date(journey.completedAt), 'MMM d, yyyy')}
                </span>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}

function LoadingSkeleton() {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <Card key={i}>
          <CardHeader className="pb-2">
            <div className="flex justify-between">
              <div className="space-y-2">
                <Skeleton className="h-5 w-40" />
                <Skeleton className="h-4 w-64" />
              </div>
              <Skeleton className="h-6 w-20" />
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div>
                <div className="flex justify-between mb-1">
                  <Skeleton className="h-4 w-16" />
                  <Skeleton className="h-4 w-20" />
                </div>
                <Skeleton className="h-2 w-full" />
              </div>
              <Skeleton className="h-4 w-32" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export function MyJourneys() {
  const { data: journeys, isLoading } = useMyJourneys();

  const activeJourneys = journeys?.filter((j) => j.status === 'Active') || [];
  const completedJourneys = journeys?.filter((j) => j.status === 'Completed') || [];
  const otherJourneys = journeys?.filter((j) => j.status !== 'Active' && j.status !== 'Completed') || [];

  if (isLoading) {
    return (
      <div className="space-y-8 p-6">
        <div>
          <h1 className="text-2xl font-bold">My Journeys</h1>
          <p className="text-muted-foreground">Your personal learning and onboarding experiences</p>
        </div>
        <LoadingSkeleton />
      </div>
    );
  }

  if (!journeys || journeys.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] gap-4">
        <Route className="h-16 w-16 text-muted-foreground" />
        <div className="text-center">
          <h2 className="text-xl font-semibold">No Journeys Yet</h2>
          <p className="text-muted-foreground mt-1">
            You haven't been enrolled in any journeys yet.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 p-6">
      <div>
        <h1 className="text-2xl font-bold">My Journeys</h1>
        <p className="text-muted-foreground">Your personal learning and onboarding experiences</p>
      </div>

      {activeJourneys.length > 0 && (
        <div className="space-y-4">
          <h2 className="text-lg font-semibold">In Progress</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {activeJourneys.map((journey) => (
              <JourneyCard key={journey.enrollmentId} journey={journey} />
            ))}
          </div>
        </div>
      )}

      {completedJourneys.length > 0 && (
        <div className="space-y-4">
          <h2 className="text-lg font-semibold">Completed</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {completedJourneys.map((journey) => (
              <JourneyCard key={journey.enrollmentId} journey={journey} />
            ))}
          </div>
        </div>
      )}

      {otherJourneys.length > 0 && (
        <div className="space-y-4">
          <h2 className="text-lg font-semibold">Other</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {otherJourneys.map((journey) => (
              <JourneyCard key={journey.enrollmentId} journey={journey} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
