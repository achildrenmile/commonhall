'use client';

import { useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { format } from 'date-fns';
import {
  ChevronLeft,
  Users,
  PlayCircle,
  CheckCircle2,
  XCircle,
  Clock,
  TrendingUp,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  FunnelChart,
  Funnel,
  LabelList,
  Cell,
} from 'recharts';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { useJourney, useJourneyAnalytics } from '../api';

interface JourneyAnalyticsProps {
  id: string;
}

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

function StatCard({
  title,
  value,
  subValue,
  icon: Icon,
  trend,
}: {
  title: string;
  value: string | number;
  subValue?: string;
  icon: React.ElementType;
  trend?: 'up' | 'down' | null;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="flex items-baseline gap-2">
          <span className="text-2xl font-bold">{value}</span>
          {trend && (
            <TrendingUp
              className={`h-4 w-4 ${trend === 'up' ? 'text-green-500' : 'text-red-500 rotate-180'}`}
            />
          )}
        </div>
        {subValue && <p className="text-xs text-muted-foreground mt-1">{subValue}</p>}
      </CardContent>
    </Card>
  );
}

function StatCardSkeleton() {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-4 w-4" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-8 w-20 mb-1" />
        <Skeleton className="h-3 w-32" />
      </CardContent>
    </Card>
  );
}

export function JourneyAnalytics({ id }: JourneyAnalyticsProps) {
  const router = useRouter();
  const { data: journey, isLoading: isLoadingJourney } = useJourney(id);
  const { data: analytics, isLoading: isLoadingAnalytics } = useJourneyAnalytics(id);

  const isLoading = isLoadingJourney || isLoadingAnalytics;

  const funnelData = useMemo(() => {
    if (!analytics?.stepFunnel) return [];
    return analytics.stepFunnel.map((step, index) => ({
      name: `Step ${index + 1}: ${step.stepTitle}`,
      value: step.delivered,
      completed: step.completed,
      fill: COLORS[index % COLORS.length],
    }));
  }, [analytics?.stepFunnel]);

  const timelineData = useMemo(() => {
    if (!analytics?.enrollmentTimeline) return [];
    return analytics.enrollmentTimeline.map((point) => ({
      date: format(new Date(point.date), 'MMM d'),
      enrollments: point.newEnrollments,
      completions: point.completions,
    }));
  }, [analytics?.enrollmentTimeline]);

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
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          <StatCardSkeleton />
          <StatCardSkeleton />
          <StatCardSkeleton />
          <StatCardSkeleton />
          <StatCardSkeleton />
        </div>
      </div>
    );
  }

  if (!journey || !analytics) {
    return (
      <div className="flex flex-col items-center justify-center h-96 gap-4">
        <AlertCircle className="h-12 w-12 text-muted-foreground" />
        <p className="text-muted-foreground">Journey not found</p>
        <Button variant="outline" onClick={() => router.push('/studio/journeys')}>
          Back to Journeys
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => router.push('/studio/journeys')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold">{journey.name} - Analytics</h1>
          <p className="text-muted-foreground">Performance metrics and insights</p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        <StatCard
          title="Total Enrollments"
          value={analytics.totalEnrollments.toLocaleString()}
          icon={Users}
        />
        <StatCard
          title="Active"
          value={analytics.activeEnrollments.toLocaleString()}
          subValue="Currently in progress"
          icon={PlayCircle}
        />
        <StatCard
          title="Completed"
          value={analytics.completedEnrollments.toLocaleString()}
          subValue={`${analytics.completionRate.toFixed(1)}% completion rate`}
          icon={CheckCircle2}
          trend={analytics.completionRate >= 50 ? 'up' : analytics.completionRate >= 25 ? null : 'down'}
        />
        <StatCard
          title="Cancelled"
          value={analytics.cancelledEnrollments.toLocaleString()}
          icon={XCircle}
        />
        <StatCard
          title="Avg. Completion"
          value={`${analytics.averageCompletionDays.toFixed(1)} days`}
          subValue="Time to complete"
          icon={Clock}
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Step Funnel */}
        <Card>
          <CardHeader>
            <CardTitle>Step Funnel</CardTitle>
            <CardDescription>Progression through journey steps</CardDescription>
          </CardHeader>
          <CardContent>
            {funnelData.length > 0 ? (
              <div className="space-y-3">
                {analytics.stepFunnel.map((step, index) => (
                  <div key={index} className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span className="font-medium">
                        Step {index + 1}: {step.stepTitle}
                      </span>
                      <span className="text-muted-foreground">
                        {step.completed}/{step.delivered} ({step.completionRate.toFixed(0)}%)
                      </span>
                    </div>
                    <div className="h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                      <div
                        className="h-full rounded-full transition-all"
                        style={{
                          width: `${step.completionRate}%`,
                          backgroundColor: COLORS[index % COLORS.length],
                        }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="h-[200px] flex items-center justify-center text-muted-foreground">
                No step data available yet
              </div>
            )}
          </CardContent>
        </Card>

        {/* Enrollment Timeline */}
        <Card>
          <CardHeader>
            <CardTitle>Enrollment Timeline</CardTitle>
            <CardDescription>New enrollments and completions (last 30 days)</CardDescription>
          </CardHeader>
          <CardContent>
            {timelineData.some((d) => d.enrollments > 0 || d.completions > 0) ? (
              <ResponsiveContainer width="100%" height={250}>
                <LineChart data={timelineData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="date" tick={{ fontSize: 12 }} tickLine={false} />
                  <YAxis tick={{ fontSize: 12 }} tickLine={false} axisLine={false} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: 'hsl(var(--popover))',
                      border: '1px solid hsl(var(--border))',
                      borderRadius: '8px',
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="enrollments"
                    stroke="#3b82f6"
                    strokeWidth={2}
                    dot={false}
                    name="Enrollments"
                  />
                  <Line
                    type="monotone"
                    dataKey="completions"
                    stroke="#22c55e"
                    strokeWidth={2}
                    dot={false}
                    name="Completions"
                  />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[250px] flex items-center justify-center text-muted-foreground">
                No enrollment data available yet
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Step Performance Table */}
      <Card>
        <CardHeader>
          <CardTitle>Step Performance</CardTitle>
          <CardDescription>Detailed metrics for each journey step</CardDescription>
        </CardHeader>
        <CardContent>
          {analytics.stepFunnel.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2 font-medium text-muted-foreground">Step</th>
                    <th className="text-right py-2 font-medium text-muted-foreground">Delivered</th>
                    <th className="text-right py-2 font-medium text-muted-foreground">Completed</th>
                    <th className="text-right py-2 font-medium text-muted-foreground">Completion Rate</th>
                    <th className="text-right py-2 font-medium text-muted-foreground">Drop-off</th>
                  </tr>
                </thead>
                <tbody>
                  {analytics.stepFunnel.map((step, index) => {
                    const prevDelivered = index > 0 ? analytics.stepFunnel[index - 1].delivered : step.delivered;
                    const dropoff = prevDelivered > 0
                      ? ((prevDelivered - step.delivered) / prevDelivered * 100).toFixed(1)
                      : '0';
                    return (
                      <tr key={index} className="border-b last:border-0">
                        <td className="py-3 font-medium">
                          {index + 1}. {step.stepTitle}
                        </td>
                        <td className="py-3 text-right">{step.delivered}</td>
                        <td className="py-3 text-right">{step.completed}</td>
                        <td className="py-3 text-right">
                          <span className={step.completionRate >= 70 ? 'text-green-600' : step.completionRate >= 40 ? 'text-yellow-600' : 'text-red-600'}>
                            {step.completionRate.toFixed(1)}%
                          </span>
                        </td>
                        <td className="py-3 text-right text-muted-foreground">
                          {index > 0 ? `${dropoff}%` : '-'}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="py-12 text-center text-muted-foreground">
              No step data available yet
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
