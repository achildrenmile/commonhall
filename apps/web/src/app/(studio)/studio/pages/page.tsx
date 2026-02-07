'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Plus, Folder, Search, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { PageTree } from '@/features/studio/components/page-tree';
import {
  usePageTree,
  useCreatePage,
  useDeletePage,
  usePublishPage,
  useUnpublishPage,
} from '@/features/studio/api/pages';
import { useStudioSpaces } from '@/features/studio/api/news';

function TreeSkeleton() {
  return (
    <div className="space-y-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <div key={i} className="space-y-2">
          <div className="flex items-center gap-2 px-2 py-2">
            <Skeleton className="h-4 w-4" />
            <Skeleton className="h-5 w-5 rounded" />
            <Skeleton className="h-5 w-32" />
          </div>
          <div className="ml-6 space-y-2">
            {Array.from({ length: 2 }).map((_, j) => (
              <div key={j} className="flex items-center gap-2 px-2 py-1.5">
                <Skeleton className="h-3.5 w-3.5" />
                <Skeleton className="h-4 w-4" />
                <Skeleton className="h-4 w-24" />
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export default function StudioPagesPage() {
  const router = useRouter();
  const [searchQuery, setSearchQuery] = useState('');
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [createSpaceId, setCreateSpaceId] = useState('');
  const [createParentId, setCreateParentId] = useState<string | undefined>();
  const [newPageTitle, setNewPageTitle] = useState('');

  const { data: spaces = [], isLoading: spacesLoading } = usePageTree();
  const { data: allSpaces = [] } = useStudioSpaces();
  const createPage = useCreatePage();
  const deletePage = useDeletePage();
  const publishPage = usePublishPage();
  const unpublishPage = useUnpublishPage();

  const handleOpenCreateDialog = (spaceId: string, parentId?: string) => {
    setCreateSpaceId(spaceId);
    setCreateParentId(parentId);
    setNewPageTitle('');
    setCreateDialogOpen(true);
  };

  const handleCreatePage = async () => {
    if (!newPageTitle.trim() || !createSpaceId) return;

    try {
      const result = await createPage.mutateAsync({
        title: newPageTitle.trim(),
        spaceId: createSpaceId,
        parentId: createParentId,
      });
      setCreateDialogOpen(false);
      router.push(`/studio/pages/${result.id}`);
    } catch (error) {
      console.error('Failed to create page:', error);
    }
  };

  const handleDeletePage = async (pageId: string) => {
    if (!confirm('Are you sure you want to delete this page? This cannot be undone.')) {
      return;
    }
    await deletePage.mutateAsync(pageId);
  };

  const handlePublishPage = async (pageId: string) => {
    await publishPage.mutateAsync(pageId);
  };

  const handleUnpublishPage = async (pageId: string) => {
    await unpublishPage.mutateAsync(pageId);
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
            Pages
          </h1>
          <p className="text-slate-600 dark:text-slate-400">
            Organize and manage your content pages
          </p>
        </div>
        <Button onClick={() => handleOpenCreateDialog((allSpaces as Array<{ id: string }>)[0]?.id || '')}>
          <Plus className="h-4 w-4 mr-2" />
          New Page
        </Button>
      </div>

      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
        <Input
          placeholder="Search pages..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-9"
        />
      </div>

      {/* Page Tree */}
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-4">
        {spacesLoading ? (
          <TreeSkeleton />
        ) : (
          <PageTree
            spaces={spaces}
            onCreatePage={handleOpenCreateDialog}
            onDeletePage={handleDeletePage}
            onPublishPage={handlePublishPage}
            onUnpublishPage={handleUnpublishPage}
          />
        )}
      </div>

      {/* Create Page Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Page</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Page Title</Label>
              <Input
                placeholder="Enter page title..."
                value={newPageTitle}
                onChange={(e) => setNewPageTitle(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleCreatePage();
                }}
                autoFocus
              />
            </div>
            <div className="space-y-2">
              <Label>Space</Label>
              <Select value={createSpaceId} onValueChange={setCreateSpaceId}>
                <SelectTrigger>
                  <SelectValue placeholder="Select space..." />
                </SelectTrigger>
                <SelectContent>
                  {(allSpaces as Array<{ id: string; name: string }>).map((space) => (
                    <SelectItem key={space.id} value={space.id}>
                      {space.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <Button
              onClick={handleCreatePage}
              disabled={!newPageTitle.trim() || !createSpaceId || createPage.isPending}
              className="w-full"
            >
              {createPage.isPending ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating...
                </>
              ) : (
                'Create Page'
              )}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
