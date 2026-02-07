'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  Plus,
  Search,
  MoreHorizontal,
  Pencil,
  Trash2,
  Archive,
  Eye,
  Loader2,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Skeleton } from '@/components/ui/skeleton';
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
import {
  useStudioNewsList,
  useDeleteArticle,
  useArchiveArticle,
  useBulkDeleteArticles,
  useBulkArchiveArticles,
  type ArticleStatus,
  type StudioNewsArticle,
} from '@/features/studio/api/news';
import { formatDate, formatRelativeTime } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';

const statusColors: Record<ArticleStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  Published: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  Scheduled: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  Archived: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
};

function ArticleRow({
  article,
  isSelected,
  onSelect,
}: {
  article: StudioNewsArticle;
  isSelected: boolean;
  onSelect: (selected: boolean) => void;
}) {
  const router = useRouter();
  const deleteArticle = useDeleteArticle();
  const archiveArticle = useArchiveArticle();

  const author = article.displayAuthor || article.author;
  const authorInitials = `${author.firstName?.[0] || ''}${author.lastName?.[0] || ''}`.toUpperCase();

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this article?')) return;
    await deleteArticle.mutateAsync(article.id);
  };

  const handleArchive = async () => {
    await archiveArticle.mutateAsync(article.id);
  };

  return (
    <TableRow className="group">
      <TableCell className="w-12">
        <Checkbox
          checked={isSelected}
          onCheckedChange={onSelect}
        />
      </TableCell>
      <TableCell>
        <div className="flex flex-col">
          <Link
            href={`/studio/news/${article.id}`}
            className="font-medium text-slate-900 dark:text-slate-100 hover:text-blue-600 dark:hover:text-blue-400 line-clamp-1"
          >
            {article.title || 'Untitled'}
          </Link>
          {article.teaserText && (
            <span className="text-sm text-slate-500 line-clamp-1">
              {article.teaserText}
            </span>
          )}
        </div>
      </TableCell>
      <TableCell>
        {article.channel ? (
          <Badge variant="outline">{article.channel.name}</Badge>
        ) : (
          <span className="text-slate-400">—</span>
        )}
      </TableCell>
      <TableCell>
        <div className="flex items-center gap-2">
          <Avatar className="h-6 w-6">
            <AvatarImage src={author.avatarUrl} />
            <AvatarFallback className="text-xs">{authorInitials}</AvatarFallback>
          </Avatar>
          <span className="text-sm text-slate-600 dark:text-slate-400">
            {author.displayName || `${author.firstName} ${author.lastName}`}
          </span>
        </div>
      </TableCell>
      <TableCell>
        <Badge className={statusColors[article.status]}>
          {article.status}
        </Badge>
      </TableCell>
      <TableCell className="text-sm text-slate-500">
        {article.publishedAt
          ? formatRelativeTime(article.publishedAt)
          : article.scheduledAt
          ? `Scheduled: ${formatDate(article.scheduledAt)}`
          : '—'}
      </TableCell>
      <TableCell className="text-sm text-slate-500 text-right">
        {article.viewCount.toLocaleString()}
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
            <DropdownMenuItem onClick={() => router.push(`/studio/news/${article.id}`)}>
              <Pencil className="mr-2 h-4 w-4" />
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => window.open(`/news/${article.slug}`, '_blank')}>
              <Eye className="mr-2 h-4 w-4" />
              View
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            {article.status !== 'Archived' && (
              <DropdownMenuItem onClick={handleArchive}>
                <Archive className="mr-2 h-4 w-4" />
                Archive
              </DropdownMenuItem>
            )}
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
          <TableCell><Skeleton className="h-5 w-64" /></TableCell>
          <TableCell><Skeleton className="h-5 w-20" /></TableCell>
          <TableCell><Skeleton className="h-6 w-32" /></TableCell>
          <TableCell><Skeleton className="h-5 w-16" /></TableCell>
          <TableCell><Skeleton className="h-4 w-20" /></TableCell>
          <TableCell><Skeleton className="h-4 w-12" /></TableCell>
          <TableCell><Skeleton className="h-8 w-8" /></TableCell>
        </TableRow>
      ))}
    </>
  );
}

export default function StudioNewsPage() {
  const [searchInput, setSearchInput] = useState('');
  const [statusFilter, setStatusFilter] = useState<ArticleStatus | 'all'>('all');
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const debouncedSearch = useDebounce(searchInput, 300);

  const filters = {
    status: statusFilter === 'all' ? undefined : statusFilter,
    search: debouncedSearch || undefined,
  };

  const { data, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage } = useStudioNewsList(filters);
  const bulkDelete = useBulkDeleteArticles();
  const bulkArchive = useBulkArchiveArticles();

  const articles = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  const totalCount = data?.pages[0]?.totalCount || 0;

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedIds(new Set(articles.map((a) => a.id)));
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

  const handleBulkDelete = async () => {
    if (!confirm(`Delete ${selectedIds.size} articles?`)) return;
    await bulkDelete.mutateAsync(Array.from(selectedIds));
    setSelectedIds(new Set());
  };

  const handleBulkArchive = async () => {
    await bulkArchive.mutateAsync(Array.from(selectedIds));
    setSelectedIds(new Set());
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
            News Articles
          </h1>
          <p className="text-slate-600 dark:text-slate-400">
            Manage your news content
          </p>
        </div>
        <Button asChild>
          <Link href="/studio/news/new">
            <Plus className="h-4 w-4 mr-2" />
            New Article
          </Link>
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4">
        <Tabs
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as ArticleStatus | 'all')}
        >
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="Draft">Draft</TabsTrigger>
            <TabsTrigger value="Published">Published</TabsTrigger>
            <TabsTrigger value="Scheduled">Scheduled</TabsTrigger>
            <TabsTrigger value="Archived">Archived</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search articles..."
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
          <Button variant="outline" size="sm" onClick={handleBulkArchive}>
            <Archive className="h-4 w-4 mr-2" />
            Archive
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

      {/* Results Count */}
      <p className="text-sm text-slate-500">
        {totalCount.toLocaleString()} {totalCount === 1 ? 'article' : 'articles'}
      </p>

      {/* Table */}
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">
                <Checkbox
                  checked={articles.length > 0 && selectedIds.size === articles.length}
                  onCheckedChange={handleSelectAll}
                />
              </TableHead>
              <TableHead>Title</TableHead>
              <TableHead>Channel</TableHead>
              <TableHead>Author</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Published</TableHead>
              <TableHead className="text-right">Views</TableHead>
              <TableHead className="w-12"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeleton />
            ) : articles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="text-center py-12 text-slate-500">
                  No articles found
                </TableCell>
              </TableRow>
            ) : (
              articles.map((article) => (
                <ArticleRow
                  key={article.id}
                  article={article}
                  isSelected={selectedIds.has(article.id)}
                  onSelect={(selected) => handleSelect(article.id, selected)}
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
