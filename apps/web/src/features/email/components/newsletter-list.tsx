'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { format } from 'date-fns';
import {
  Plus,
  MoreHorizontal,
  Send,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  FileEdit,
  Eye,
  Trash2,
  BarChart3,
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
import { useNewsletters, useDeleteNewsletter } from '../api';
import type { Newsletter, NewsletterStatus } from '../types';

const statusConfig: Record<NewsletterStatus, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline'; icon: React.ElementType }> = {
  Draft: { label: 'Draft', variant: 'secondary', icon: FileEdit },
  Scheduled: { label: 'Scheduled', variant: 'outline', icon: Clock },
  Sending: { label: 'Sending', variant: 'default', icon: Loader2 },
  Sent: { label: 'Sent', variant: 'default', icon: CheckCircle2 },
  Failed: { label: 'Failed', variant: 'destructive', icon: XCircle },
};

function StatusBadge({ status }: { status: NewsletterStatus }) {
  const config = statusConfig[status];
  const Icon = config.icon;
  return (
    <Badge variant={config.variant} className="gap-1">
      <Icon className={`h-3 w-3 ${status === 'Sending' ? 'animate-spin' : ''}`} />
      {config.label}
    </Badge>
  );
}

function NewsletterRow({ newsletter, onDelete }: { newsletter: Newsletter; onDelete: () => void }) {
  const router = useRouter();

  return (
    <TableRow>
      <TableCell>
        <div>
          <Link
            href={`/studio/email/${newsletter.id}`}
            className="font-medium hover:text-primary transition-colors"
          >
            {newsletter.title}
          </Link>
          <p className="text-sm text-muted-foreground">{newsletter.subject}</p>
        </div>
      </TableCell>
      <TableCell>
        <StatusBadge status={newsletter.status} />
      </TableCell>
      <TableCell className="text-muted-foreground">
        {newsletter.scheduledAt
          ? format(new Date(newsletter.scheduledAt), 'MMM d, yyyy h:mm a')
          : newsletter.sentAt
          ? format(new Date(newsletter.sentAt), 'MMM d, yyyy h:mm a')
          : format(new Date(newsletter.createdAt), 'MMM d, yyyy')}
      </TableCell>
      <TableCell className="text-right">
        {newsletter.recipientCount > 0 ? newsletter.recipientCount.toLocaleString() : '-'}
      </TableCell>
      <TableCell className="text-right">
        {newsletter.status === 'Sent' ? (
          <span className={newsletter.openRate >= 30 ? 'text-green-600' : newsletter.openRate >= 15 ? 'text-yellow-600' : ''}>
            {newsletter.openRate.toFixed(1)}%
          </span>
        ) : (
          '-'
        )}
      </TableCell>
      <TableCell className="text-right">
        {newsletter.status === 'Sent' ? (
          <span className={newsletter.clickRate >= 5 ? 'text-green-600' : newsletter.clickRate >= 2 ? 'text-yellow-600' : ''}>
            {newsletter.clickRate.toFixed(1)}%
          </span>
        ) : (
          '-'
        )}
      </TableCell>
      <TableCell>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => router.push(`/studio/email/${newsletter.id}`)}>
              <FileEdit className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => router.push(`/studio/email/${newsletter.id}/preview`)}>
              <Eye className="h-4 w-4 mr-2" />
              Preview
            </DropdownMenuItem>
            {newsletter.status === 'Sent' && (
              <DropdownMenuItem onClick={() => router.push(`/studio/email/${newsletter.id}/analytics`)}>
                <BarChart3 className="h-4 w-4 mr-2" />
                Analytics
              </DropdownMenuItem>
            )}
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
          <TableCell><Skeleton className="h-6 w-20" /></TableCell>
          <TableCell><Skeleton className="h-4 w-28" /></TableCell>
          <TableCell><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
          <TableCell><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
          <TableCell><Skeleton className="h-4 w-12 ml-auto" /></TableCell>
          <TableCell><Skeleton className="h-8 w-8 rounded" /></TableCell>
        </TableRow>
      ))}
    </>
  );
}

export function NewsletterList() {
  const [statusFilter, setStatusFilter] = useState<NewsletterStatus | 'all'>('all');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useNewsletters(
    statusFilter === 'all' ? undefined : statusFilter
  );

  const deleteMutation = useDeleteNewsletter();

  const newsletters = data?.pages.flatMap((page) => page.items) ?? [];

  const handleDelete = async () => {
    if (!deleteId) return;
    await deleteMutation.mutateAsync(deleteId);
    setDeleteId(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Newsletters</h1>
          <p className="text-muted-foreground">Create and manage email newsletters</p>
        </div>
        <Button asChild>
          <Link href="/studio/email/new">
            <Plus className="h-4 w-4 mr-2" />
            New Newsletter
          </Link>
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <Select
          value={statusFilter}
          onValueChange={(value) => setStatusFilter(value as NewsletterStatus | 'all')}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Filter by status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Draft">Draft</SelectItem>
            <SelectItem value="Scheduled">Scheduled</SelectItem>
            <SelectItem value="Sending">Sending</SelectItem>
            <SelectItem value="Sent">Sent</SelectItem>
            <SelectItem value="Failed">Failed</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Newsletter</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Date</TableHead>
              <TableHead className="text-right">Recipients</TableHead>
              <TableHead className="text-right">Open Rate</TableHead>
              <TableHead className="text-right">Click Rate</TableHead>
              <TableHead className="w-10"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <LoadingSkeleton />
            ) : newsletters.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center">
                  <div className="flex flex-col items-center gap-2">
                    <Send className="h-8 w-8 text-muted-foreground" />
                    <p className="text-muted-foreground">No newsletters found</p>
                    <Button asChild variant="outline" size="sm">
                      <Link href="/studio/email/new">Create your first newsletter</Link>
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              newsletters.map((newsletter) => (
                <NewsletterRow
                  key={newsletter.id}
                  newsletter={newsletter}
                  onDelete={() => setDeleteId(newsletter.id)}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {hasNextPage && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
          >
            {isFetchingNextPage ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Loading...
              </>
            ) : (
              'Load More'
            )}
          </Button>
        </div>
      )}

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Newsletter?</AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. The newsletter and all its analytics data will be
              permanently deleted.
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
