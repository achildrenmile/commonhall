'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
  Plus,
  ClipboardList,
  MoreHorizontal,
  Trash2,
  BarChart2,
  Play,
  Pause,
  Loader2,
  Users,
  Calendar,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useToast } from '@/hooks/use-toast';
import { formatRelativeTime } from '@/lib/utils';
import { useSurveys, useDeleteSurvey, useActivateSurvey, useCloseSurvey } from '../api';
import type { SurveyStatus, SurveyListItem } from '../types';

const statusColors: Record<SurveyStatus, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  Draft: { label: 'Draft', variant: 'secondary' },
  Active: { label: 'Active', variant: 'default' },
  Closed: { label: 'Closed', variant: 'outline' },
  Archived: { label: 'Archived', variant: 'destructive' },
};

export function SurveyList() {
  const { toast } = useToast();
  const [statusFilter, setStatusFilter] = useState<SurveyStatus | 'all'>('all');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: surveys, isLoading } = useSurveys(statusFilter === 'all' ? undefined : statusFilter);
  const deleteMutation = useDeleteSurvey();
  const activateMutation = useActivateSurvey();
  const closeMutation = useCloseSurvey();

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      await deleteMutation.mutateAsync(deleteId);
      toast({ title: 'Survey deleted' });
      setDeleteId(null);
    } catch {
      toast({ title: 'Error', description: 'Failed to delete survey', variant: 'destructive' });
    }
  };

  const handleToggleStatus = async (survey: SurveyListItem) => {
    try {
      if (survey.status === 'Active') {
        await closeMutation.mutateAsync(survey.id);
        toast({ title: 'Survey closed' });
      } else if (survey.status === 'Draft') {
        await activateMutation.mutateAsync(survey.id);
        toast({ title: 'Survey activated' });
      }
    } catch (error) {
      toast({ title: 'Error', description: 'Failed to update survey', variant: 'destructive' });
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Surveys</h1>
          <p className="text-sm text-muted-foreground">
            Create and manage employee surveys
          </p>
        </div>
        <Button asChild>
          <Link href="/studio/surveys/new">
            <Plus className="h-4 w-4 mr-2" />
            Create Survey
          </Link>
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as SurveyStatus | 'all')}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Draft">Draft</SelectItem>
            <SelectItem value="Active">Active</SelectItem>
            <SelectItem value="Closed">Closed</SelectItem>
            <SelectItem value="Archived">Archived</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {!surveys?.length ? (
        <div className="text-center py-16 bg-muted/30 rounded-lg border border-dashed">
          <ClipboardList className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="font-semibold mb-2">No surveys yet</h3>
          <p className="text-sm text-muted-foreground mb-4">
            Create your first survey to gather feedback from employees
          </p>
          <Button asChild>
            <Link href="/studio/surveys/new">
              <Plus className="h-4 w-4 mr-2" />
              Create Survey
            </Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-4">
          {surveys.map((survey) => (
            <div
              key={survey.id}
              className="border rounded-lg p-4 hover:border-foreground/20 transition-colors bg-card"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <Link
                      href={`/studio/surveys/${survey.id}`}
                      className="font-semibold hover:underline truncate"
                    >
                      {survey.title}
                    </Link>
                    <Badge variant={statusColors[survey.status].variant}>
                      {statusColors[survey.status].label}
                    </Badge>
                    {survey.isAnonymous && (
                      <Badge variant="outline" className="text-xs">Anonymous</Badge>
                    )}
                  </div>
                  {survey.description && (
                    <p className="text-sm text-muted-foreground line-clamp-1 mb-2">
                      {survey.description}
                    </p>
                  )}
                  <div className="flex items-center gap-4 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <ClipboardList className="h-3 w-3" />
                      {survey.questionCount} questions
                    </span>
                    <span className="flex items-center gap-1">
                      <Users className="h-3 w-3" />
                      {survey.responseCount} responses
                    </span>
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      Created {formatRelativeTime(survey.createdAt)}
                    </span>
                  </div>
                </div>

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-8 w-8">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem asChild>
                      <Link href={`/studio/surveys/${survey.id}`}>
                        Edit
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href={`/studio/surveys/${survey.id}/analytics`}>
                        <BarChart2 className="h-4 w-4 mr-2" />
                        Analytics
                      </Link>
                    </DropdownMenuItem>
                    {survey.status === 'Draft' && (
                      <DropdownMenuItem
                        onClick={() => handleToggleStatus(survey)}
                        disabled={survey.questionCount === 0}
                      >
                        <Play className="h-4 w-4 mr-2" />
                        Activate
                      </DropdownMenuItem>
                    )}
                    {survey.status === 'Active' && (
                      <DropdownMenuItem onClick={() => handleToggleStatus(survey)}>
                        <Pause className="h-4 w-4 mr-2" />
                        Close
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      className="text-destructive"
                      onClick={() => setDeleteId(survey.id)}
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
          ))}
        </div>
      )}

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Survey</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this survey and all its responses. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
