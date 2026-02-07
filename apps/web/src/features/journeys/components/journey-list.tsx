'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { format } from 'date-fns';
import {
  Plus,
  MoreHorizontal,
  Play,
  Pause,
  Edit,
  BarChart3,
  Trash2,
  Users,
  Loader2,
  Route,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
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
import { Skeleton } from '@/components/ui/skeleton';
import { useJourneys, useDeleteJourney, useActivateJourney, useDeactivateJourney } from '../api';
import type { Journey, JourneyTriggerType } from '../types';

const triggerTypeLabels: Record<JourneyTriggerType, string> = {
  Manual: 'Manual',
  Onboarding: 'Onboarding',
  RoleChange: 'Role Change',
  LocationChange: 'Location',
  DateBased: 'Date-Based',
  GroupJoin: 'Group Join',
};

function JourneyRow({
  journey,
  onDelete,
}: {
  journey: Journey;
  onDelete: () => void;
}) {
  const router = useRouter();
  const activateMutation = useActivateJourney();
  const deactivateMutation = useDeactivateJourney();

  const handleToggleActive = async () => {
    if (journey.isActive) {
      await deactivateMutation.mutateAsync(journey.id);
    } else {
      await activateMutation.mutateAsync(journey.id);
    }
  };

  return (
    <TableRow>
      <TableCell>
        <div>
          <Link
            href={`/studio/journeys/${journey.id}`}
            className="font-medium hover:text-primary transition-colors"
          >
            {journey.name}
          </Link>
          {journey.description && (
            <p className="text-sm text-muted-foreground line-clamp-1">{journey.description}</p>
          )}
        </div>
      </TableCell>
      <TableCell>
        <Badge variant={journey.isActive ? 'default' : 'secondary'}>
          {journey.isActive ? 'Active' : 'Inactive'}
        </Badge>
      </TableCell>
      <TableCell>
        <Badge variant="outline">{triggerTypeLabels[journey.triggerType]}</Badge>
      </TableCell>
      <TableCell className="text-center">{journey.stepCount}</TableCell>
      <TableCell className="text-center">{journey.enrollmentCount}</TableCell>
      <TableCell className="text-center">
        {journey.enrollmentCount > 0 ? (
          <span className={journey.completionRate >= 50 ? 'text-green-600' : ''}>
            {journey.completionRate.toFixed(0)}%
          </span>
        ) : (
          '-'
        )}
      </TableCell>
      <TableCell className="text-muted-foreground">
        {format(new Date(journey.createdAt), 'MMM d, yyyy')}
      </TableCell>
      <TableCell>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => router.push(`/studio/journeys/${journey.id}`)}>
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => router.push(`/studio/journeys/${journey.id}/analytics`)}>
              <BarChart3 className="h-4 w-4 mr-2" />
              Analytics
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => router.push(`/studio/journeys/${journey.id}/enrollments`)}>
              <Users className="h-4 w-4 mr-2" />
              Enrollments
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={handleToggleActive}>
              {journey.isActive ? (
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
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem onClick={onDelete} className="text-destructive">
              <Trash2 className="h-4 w-4 mr-2" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  );
}

function LoadingSkeleton() {
  return (
    <>
      {Array.from({ length: 5 }).map((_, i) => (
        <TableRow key={i}>
          <TableCell>
            <Skeleton className="h-4 w-48 mb-2" />
            <Skeleton className="h-3 w-32" />
          </TableCell>
          <TableCell><Skeleton className="h-6 w-16" /></TableCell>
          <TableCell><Skeleton className="h-6 w-20" /></TableCell>
          <TableCell><Skeleton className="h-4 w-8 mx-auto" /></TableCell>
          <TableCell><Skeleton className="h-4 w-10 mx-auto" /></TableCell>
          <TableCell><Skeleton className="h-4 w-10 mx-auto" /></TableCell>
          <TableCell><Skeleton className="h-4 w-24" /></TableCell>
          <TableCell><Skeleton className="h-8 w-8 rounded" /></TableCell>
        </TableRow>
      ))}
    </>
  );
}

export function JourneyList() {
  const [activeFilter, setActiveFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: journeys, isLoading } = useJourneys(
    activeFilter === 'all' ? undefined : activeFilter === 'active'
  );

  const deleteMutation = useDeleteJourney();

  const handleDelete = async () => {
    if (!deleteId) return;
    await deleteMutation.mutateAsync(deleteId);
    setDeleteId(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Employee Journeys</h1>
          <p className="text-muted-foreground">Create and manage automated employee experiences</p>
        </div>
        <Button asChild>
          <Link href="/studio/journeys/new">
            <Plus className="h-4 w-4 mr-2" />
            New Journey
          </Link>
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <Select
          value={activeFilter}
          onValueChange={(value) => setActiveFilter(value as typeof activeFilter)}
        >
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Journeys</SelectItem>
            <SelectItem value="active">Active Only</SelectItem>
            <SelectItem value="inactive">Inactive Only</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Journey</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Trigger</TableHead>
              <TableHead className="text-center">Steps</TableHead>
              <TableHead className="text-center">Enrollments</TableHead>
              <TableHead className="text-center">Completion</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="w-10"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <LoadingSkeleton />
            ) : !journeys || journeys.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="h-32 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <Route className="h-8 w-8 text-muted-foreground" />
                    <p className="text-muted-foreground">No journeys found</p>
                    <Button asChild variant="outline" size="sm">
                      <Link href="/studio/journeys/new">Create your first journey</Link>
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              journeys.map((journey) => (
                <JourneyRow
                  key={journey.id}
                  journey={journey}
                  onDelete={() => setDeleteId(journey.id)}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Journey?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. The journey and all enrollment data will be
              permanently deleted. Active enrollments will be cancelled.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deleteMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Deleting...
                </>
              ) : (
                'Delete'
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
