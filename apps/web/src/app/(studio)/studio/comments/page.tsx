'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import {
  Search,
  MoreHorizontal,
  Check,
  X,
  Flag,
  Trash2,
  ExternalLink,
  Loader2,
  MessageSquare,
  AlertTriangle,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { cn, formatRelativeTime } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';
import {
  useStudioComments,
  useApproveComment,
  useRejectComment,
  useDeleteComment,
  useBulkApproveComments,
  useBulkRejectComments,
  useBulkDeleteComments,
  type CommentStatus,
  type StudioComment,
} from '@/features/studio/api/comments';

const statusColors: Record<CommentStatus, string> = {
  Pending: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
  Approved: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  Flagged: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  Rejected: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
};

function CommentRow({
  comment,
  isSelected,
  onSelect,
}: {
  comment: StudioComment;
  isSelected: boolean;
  onSelect: (selected: boolean) => void;
}) {
  const approveComment = useApproveComment();
  const rejectComment = useRejectComment();
  const deleteComment = useDeleteComment();

  const authorInitials = comment.author.displayName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .substring(0, 2)
    .toUpperCase();

  const handleApprove = async () => {
    await approveComment.mutateAsync(comment.id);
  };

  const handleReject = async () => {
    await rejectComment.mutateAsync(comment.id);
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this comment?')) return;
    await deleteComment.mutateAsync(comment.id);
  };

  return (
    <TableRow className="group">
      <TableCell className="w-12">
        <Checkbox checked={isSelected} onCheckedChange={onSelect} />
      </TableCell>
      <TableCell>
        <div className="flex items-start gap-3">
          <Avatar className="h-8 w-8">
            <AvatarImage src={comment.author.avatarUrl} />
            <AvatarFallback className="text-xs">{authorInitials}</AvatarFallback>
          </Avatar>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <span className="font-medium text-sm">
                {comment.author.displayName}
              </span>
              <span className="text-xs text-slate-500">
                {formatRelativeTime(comment.createdAt)}
              </span>
            </div>
            <p className="text-sm text-slate-600 dark:text-slate-400 line-clamp-2">
              {comment.body}
            </p>
            {comment.flagReason && (
              <div className="flex items-center gap-1 mt-1 text-xs text-red-600">
                <AlertTriangle className="h-3 w-3" />
                <span>Flagged: {comment.flagReason}</span>
              </div>
            )}
          </div>
        </div>
      </TableCell>
      <TableCell>
        <Link
          href={`/news/${comment.article.slug}`}
          target="_blank"
          className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 flex items-center gap-1"
        >
          <span className="truncate max-w-[200px]">{comment.article.title}</span>
          <ExternalLink className="h-3 w-3 shrink-0" />
        </Link>
      </TableCell>
      <TableCell>
        <Badge className={statusColors[comment.status]}>
          {comment.status}
        </Badge>
      </TableCell>
      <TableCell className="w-12">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 opacity-0 group-hover:opacity-100"
            >
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {comment.status !== 'Approved' && (
              <DropdownMenuItem onClick={handleApprove}>
                <Check className="mr-2 h-4 w-4" />
                Approve
              </DropdownMenuItem>
            )}
            {comment.status !== 'Rejected' && (
              <DropdownMenuItem onClick={handleReject}>
                <X className="mr-2 h-4 w-4" />
                Reject
              </DropdownMenuItem>
            )}
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={handleDelete}
              className="text-red-600 dark:text-red-400"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  );
}

function TableSkeleton() {
  return (
    <>
      {Array.from({ length: 5 }).map((_, i) => (
        <TableRow key={i}>
          <TableCell><Skeleton className="h-4 w-4" /></TableCell>
          <TableCell>
            <div className="flex items-start gap-3">
              <Skeleton className="h-8 w-8 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-64" />
              </div>
            </div>
          </TableCell>
          <TableCell><Skeleton className="h-4 w-40" /></TableCell>
          <TableCell><Skeleton className="h-5 w-16" /></TableCell>
          <TableCell><Skeleton className="h-8 w-8" /></TableCell>
        </TableRow>
      ))}
    </>
  );
}

export default function StudioCommentsPage() {
  const [searchInput, setSearchInput] = useState('');
  const [statusFilter, setStatusFilter] = useState<CommentStatus | 'all'>('all');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const debouncedSearch = useDebounce(searchInput, 300);

  const filters = {
    status: statusFilter === 'all' ? undefined : statusFilter,
    search: debouncedSearch || undefined,
  };

  const { data, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage } = useStudioComments(filters);
  const bulkApprove = useBulkApproveComments();
  const bulkReject = useBulkRejectComments();
  const bulkDelete = useBulkDeleteComments();

  const comments = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  const statusCounts = data?.pages[0]?.statusCounts;

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedIds(new Set(comments.map((c) => c.id)));
    } else {
      setSelectedIds(new Set());
    }
  };

  const handleSelect = (id: string, selected: boolean) => {
    const newSet = new Set(selectedIds);
    if (selected) {
      newSet.add(id);
    } else {
      newSet.delete(id);
    }
    setSelectedIds(newSet);
  };

  const handleBulkApprove = async () => {
    await bulkApprove.mutateAsync(Array.from(selectedIds));
    setSelectedIds(new Set());
  };

  const handleBulkReject = async () => {
    await bulkReject.mutateAsync(Array.from(selectedIds));
    setSelectedIds(new Set());
  };

  const handleBulkDelete = async () => {
    if (!confirm(`Delete ${selectedIds.size} comments?`)) return;
    await bulkDelete.mutateAsync(Array.from(selectedIds));
    setSelectedIds(new Set());
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          Comments
        </h1>
        <p className="text-slate-600 dark:text-slate-400">
          Moderate and manage user comments
        </p>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4">
        <Tabs
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as CommentStatus | 'all')}
        >
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="Pending" className="gap-1">
              Pending
              {statusCounts?.pending ? (
                <Badge variant="secondary" className="ml-1 px-1.5 py-0 text-xs">
                  {statusCounts.pending}
                </Badge>
              ) : null}
            </TabsTrigger>
            <TabsTrigger value="Approved">Approved</TabsTrigger>
            <TabsTrigger value="Flagged" className="gap-1">
              Flagged
              {statusCounts?.flagged ? (
                <Badge variant="destructive" className="ml-1 px-1.5 py-0 text-xs">
                  {statusCounts.flagged}
                </Badge>
              ) : null}
            </TabsTrigger>
            <TabsTrigger value="Rejected">Rejected</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search comments..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Bulk Actions */}
      {selectedIds.size > 0 && (
        <div className="flex items-center gap-4 p-3 bg-slate-100 dark:bg-slate-800 rounded-lg">
          <span className="text-sm font-medium">
            {selectedIds.size} selected
          </span>
          <Button variant="outline" size="sm" onClick={handleBulkApprove}>
            <Check className="h-4 w-4 mr-2" />
            Approve
          </Button>
          <Button variant="outline" size="sm" onClick={handleBulkReject}>
            <X className="h-4 w-4 mr-2" />
            Reject
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={handleBulkDelete}
            className="text-red-600 hover:text-red-700"
          >
            <Trash2 className="h-4 w-4 mr-2" />
            Delete
          </Button>
          <Button variant="ghost" size="sm" onClick={() => setSelectedIds(new Set())}>
            Cancel
          </Button>
        </div>
      )}

      {/* Table */}
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">
                <Checkbox
                  checked={comments.length > 0 && selectedIds.size === comments.length}
                  onCheckedChange={handleSelectAll}
                />
              </TableHead>
              <TableHead>Comment</TableHead>
              <TableHead>Article</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-12"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeleton />
            ) : comments.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-12 text-slate-500">
                  <MessageSquare className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No comments found</p>
                </TableCell>
              </TableRow>
            ) : (
              comments.map((comment) => (
                <CommentRow
                  key={comment.id}
                  comment={comment}
                  isSelected={selectedIds.has(comment.id)}
                  onSelect={(selected) => handleSelect(comment.id, selected)}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Load More */}
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
              'Load more'
            )}
          </Button>
        </div>
      )}
    </div>
  );
}
