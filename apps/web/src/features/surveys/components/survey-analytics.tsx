'use client';

import { useRouter } from 'next/navigation';
import { ChevronLeft, Download, Loader2, Users, Calendar, Percent } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useSurvey, useSurveyAnalytics, exportSurveyCsv } from '../api';
import type { SurveyQuestionType, QuestionAnalytics, ChoiceAnalytics, RatingAnalytics, FreeTextAnalytics } from '../types';
import { formatRelativeTime } from '@/lib/utils';

interface SurveyAnalyticsProps {
  id: string;
}

function isChoiceAnalytics(analytics: unknown): analytics is ChoiceAnalytics {
  return typeof analytics === 'object' && analytics !== null && 'options' in analytics;
}

function isRatingAnalytics(analytics: unknown): analytics is RatingAnalytics {
  return typeof analytics === 'object' && analytics !== null && 'average' in analytics;
}

function isFreeTextAnalytics(analytics: unknown): analytics is FreeTextAnalytics {
  return typeof analytics === 'object' && analytics !== null && 'responses' in analytics;
}

function QuestionAnalyticsCard({ question }: { question: QuestionAnalytics }) {
  const totalResponses = question.totalAnswers;

  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-base">{question.questionText}</CardTitle>
            <CardDescription>
              {question.type} · {totalResponses} responses
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {isChoiceAnalytics(question.analytics) && (
          <ChoiceChart analytics={question.analytics} total={totalResponses} />
        )}
        {isRatingAnalytics(question.analytics) && (
          <RatingChart analytics={question.analytics} type={question.type} />
        )}
        {isFreeTextAnalytics(question.analytics) && (
          <FreeTextList analytics={question.analytics} />
        )}
      </CardContent>
    </Card>
  );
}

function ChoiceChart({ analytics, total }: { analytics: ChoiceAnalytics; total: number }) {
  const entries = Object.entries(analytics.options).sort((a, b) => b[1] - a[1]);

  if (entries.length === 0) {
    return <p className="text-sm text-muted-foreground">No responses yet</p>;
  }

  return (
    <div className="space-y-3">
      {entries.map(([option, count]) => {
        const percentage = total > 0 ? Math.round((count / total) * 100) : 0;
        return (
          <div key={option} className="space-y-1">
            <div className="flex items-center justify-between text-sm">
              <span className="truncate">{option}</span>
              <span className="text-muted-foreground">
                {count} ({percentage}%)
              </span>
            </div>
            <Progress value={percentage} className="h-2" />
          </div>
        );
      })}
    </div>
  );
}

function RatingChart({ analytics, type }: { analytics: RatingAnalytics; type: SurveyQuestionType }) {
  const maxValue = type === 'NPS' ? 10 : 5;
  const distribution = Object.entries(analytics.distribution)
    .map(([value, count]) => ({ value: parseInt(value), count }))
    .sort((a, b) => a.value - b.value);

  const maxCount = Math.max(...distribution.map((d) => d.count), 1);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <div className="text-center">
          <div className="text-3xl font-bold">{analytics.average.toFixed(1)}</div>
          <div className="text-xs text-muted-foreground">Average</div>
        </div>
        <div className="text-center">
          <div className="text-3xl font-bold">{analytics.count}</div>
          <div className="text-xs text-muted-foreground">Responses</div>
        </div>
      </div>

      {type === 'NPS' && (
        <div className="flex items-center gap-2 text-xs">
          <div className="flex-1 text-center px-2 py-1 bg-red-100 dark:bg-red-900/30 rounded">
            Detractors (0-6)
          </div>
          <div className="flex-1 text-center px-2 py-1 bg-yellow-100 dark:bg-yellow-900/30 rounded">
            Passives (7-8)
          </div>
          <div className="flex-1 text-center px-2 py-1 bg-green-100 dark:bg-green-900/30 rounded">
            Promoters (9-10)
          </div>
        </div>
      )}

      <div className="flex items-end gap-1 h-24">
        {Array.from({ length: maxValue + (type === 'NPS' ? 1 : 0) }, (_, i) => {
          const value = type === 'NPS' ? i : i + 1;
          const entry = distribution.find((d) => d.value === value);
          const count = entry?.count || 0;
          const height = maxCount > 0 ? (count / maxCount) * 100 : 0;

          let bgColor = 'bg-blue-500';
          if (type === 'NPS') {
            if (value <= 6) bgColor = 'bg-red-500';
            else if (value <= 8) bgColor = 'bg-yellow-500';
            else bgColor = 'bg-green-500';
          }

          return (
            <div key={value} className="flex-1 flex flex-col items-center">
              <div
                className={`w-full ${bgColor} rounded-t transition-all`}
                style={{ height: `${height}%`, minHeight: count > 0 ? '4px' : '0' }}
              />
              <span className="text-xs text-muted-foreground mt-1">{value}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function FreeTextList({ analytics }: { analytics: FreeTextAnalytics }) {
  if (analytics.responses.length === 0) {
    return <p className="text-sm text-muted-foreground">No responses yet</p>;
  }

  return (
    <div className="space-y-2 max-h-64 overflow-auto">
      {analytics.responses.map((response, idx) => (
        <div
          key={idx}
          className="p-3 bg-muted/50 rounded-lg text-sm"
        >
          {response}
        </div>
      ))}
    </div>
  );
}

export function SurveyAnalytics({ id }: SurveyAnalyticsProps) {
  const router = useRouter();
  const { data: survey, isLoading: surveyLoading } = useSurvey(id);
  const { data: analytics, isLoading: analyticsLoading } = useSurveyAnalytics(id);

  const isLoading = surveyLoading || analyticsLoading;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!survey || !analytics) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Survey not found</p>
      </div>
    );
  }

  const handleExport = () => {
    window.open(exportSurveyCsv(id), '_blank');
  };

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push(`/studio/surveys/${id}`)}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">{survey.title}</h1>
              <p className="text-sm text-muted-foreground">Analytics</p>
            </div>
            <Badge variant={survey.status === 'Active' ? 'default' : 'secondary'}>
              {survey.status}
            </Badge>
          </div>
          <Button variant="outline" onClick={handleExport}>
            <Download className="h-4 w-4 mr-2" />
            Export CSV
          </Button>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-6">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Overview Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-4">
                  <div className="p-2 bg-blue-100 dark:bg-blue-900/30 rounded-lg">
                    <Users className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold">{analytics.totalResponses}</p>
                    <p className="text-sm text-muted-foreground">Total Responses</p>
                  </div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-4">
                  <div className="p-2 bg-green-100 dark:bg-green-900/30 rounded-lg">
                    <Percent className="h-5 w-5 text-green-600 dark:text-green-400" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold">{analytics.completeResponses}</p>
                    <p className="text-sm text-muted-foreground">Complete Responses</p>
                  </div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-4">
                  <div className="p-2 bg-purple-100 dark:bg-purple-900/30 rounded-lg">
                    <Calendar className="h-5 w-5 text-purple-600 dark:text-purple-400" />
                  </div>
                  <div>
                    <p className="text-2xl font-bold">{formatRelativeTime(survey.createdAt)}</p>
                    <p className="text-sm text-muted-foreground">Created</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Question Analytics */}
          <div className="space-y-4">
            <h2 className="font-semibold text-lg">Question Breakdown</h2>
            {analytics.questionAnalytics.length === 0 ? (
              <p className="text-muted-foreground">No questions in this survey</p>
            ) : (
              analytics.questionAnalytics.map((question) => (
                <QuestionAnalyticsCard key={question.questionId} question={question} />
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
