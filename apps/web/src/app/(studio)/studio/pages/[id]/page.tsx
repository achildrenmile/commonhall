'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Save,
  Eye,
  EyeOff,
  Trash2,
  Loader2,
  Globe,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { BlockEditor } from '@/features/editor';
import type { WidgetBlock } from '@/features/pages/widgets';
import {
  useStudioPage,
  useUpdatePage,
  useDeletePage,
  usePublishPage,
  useUnpublishPage,
} from '@/features/studio/api/pages';
import { formatDate } from '@/lib/utils';

interface PageFormData {
  title: string;
  slug: string;
  icon: string;
  description: string;
  content: WidgetBlock[];
}

const defaultFormData: PageFormData = {
  title: '',
  slug: '',
  icon: '',
  description: '',
  content: [],
};

function EditorSkeleton() {
  return (
    <div className="space-y-6">
      <Skeleton className="h-10 w-full max-w-xl" />
      <Skeleton className="h-20 w-full max-w-xl" />
      <Skeleton className="h-64 w-full" />
    </div>
  );
}

export default function PageEditorPage() {
  const params = useParams();
  const router = useRouter();
  const pageId = params.id as string;

  const { data: page, isLoading: pageLoading } = useStudioPage(pageId);
  const updatePage = useUpdatePage();
  const deletePage = useDeletePage();
  const publishPage = usePublishPage();
  const unpublishPage = useUnpublishPage();

  const [formData, setFormData] = useState<PageFormData>(defaultFormData);
  const [isSaving, setIsSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  // Load page data
  useEffect(() => {
    if (page) {
      setFormData({
        title: page.title || '',
        slug: page.slug || '',
        icon: page.icon || '',
        description: page.description || '',
        content: page.content || [],
      });
    }
  }, [page]);

  const updateField = useCallback(
    <K extends keyof PageFormData>(field: K, value: PageFormData[K]) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
    },
    []
  );

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await updatePage.mutateAsync({
        id: pageId,
        ...formData,
      });
      setLastSaved(new Date());
    } catch (error) {
      console.error('Failed to save:', error);
    } finally {
      setIsSaving(false);
    }
  };

  const handlePublish = async () => {
    // Save first, then publish
    await handleSave();
    await publishPage.mutateAsync(pageId);
  };

  const handleUnpublish = async () => {
    await unpublishPage.mutateAsync(pageId);
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this page? This cannot be undone.')) {
      return;
    }
    try {
      await deletePage.mutateAsync(pageId);
      router.push('/studio/pages');
    } catch (error) {
      console.error('Failed to delete:', error);
    }
  };

  const handlePreview = () => {
    if (page) {
      window.open(`/${page.space.slug}/${page.slug}`, '_blank');
    }
  };

  if (pageLoading) {
    return (
      <div className="max-w-5xl mx-auto">
        <EditorSkeleton />
      </div>
    );
  }

  if (!page) {
    return (
      <div className="max-w-5xl mx-auto text-center py-12">
        <p className="text-slate-500">Page not found</p>
        <Button asChild className="mt-4">
          <Link href="/studio/pages">Back to Pages</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/studio/pages">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
                Edit Page
              </h1>
              <Badge variant="outline" className="font-normal">
                {page.space.name}
              </Badge>
              {page.isPublished ? (
                <Badge className="bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
                  Published
                </Badge>
              ) : (
                <Badge className="bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300">
                  Draft
                </Badge>
              )}
            </div>
            {lastSaved && (
              <p className="text-sm text-slate-500">
                Last saved {formatDate(lastSaved.toISOString())}
              </p>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={handlePreview}>
            <Globe className="mr-2 h-4 w-4" />
            Preview
          </Button>
          {page.isPublished ? (
            <Button variant="outline" onClick={handleUnpublish}>
              <EyeOff className="mr-2 h-4 w-4" />
              Unpublish
            </Button>
          ) : (
            <Button onClick={handlePublish} disabled={isSaving}>
              {isSaving ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Eye className="mr-2 h-4 w-4" />
              )}
              Publish
            </Button>
          )}
        </div>
      </div>

      {/* Editor */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr,300px] gap-6">
        {/* Main Content */}
        <div className="space-y-6">
          {/* Title */}
          <div className="space-y-2">
            <Label>Page Title</Label>
            <Input
              value={formData.title}
              onChange={(e) => updateField('title', e.target.value)}
              placeholder="Enter page title..."
              className="text-lg font-medium"
            />
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label>Description</Label>
            <Textarea
              value={formData.description}
              onChange={(e) => updateField('description', e.target.value)}
              placeholder="Brief description of this page..."
              rows={2}
            />
          </div>

          {/* Content */}
          <div className="space-y-2">
            <Label>Content</Label>
            <BlockEditor
              blocks={formData.content}
              onChange={(blocks) => updateField('content', blocks)}
            />
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-4 space-y-4">
            <h3 className="font-medium text-slate-900 dark:text-slate-100">
              Page Settings
            </h3>

            {/* Slug */}
            <div className="space-y-2">
              <Label>URL Slug</Label>
              <Input
                value={formData.slug}
                onChange={(e) => updateField('slug', e.target.value)}
                placeholder="page-url"
              />
              <p className="text-xs text-slate-500">
                /{page.space.slug}/{formData.slug || 'page-url'}
              </p>
            </div>

            {/* Icon */}
            <div className="space-y-2">
              <Label>Icon (Emoji)</Label>
              <Input
                value={formData.icon}
                onChange={(e) => updateField('icon', e.target.value)}
                placeholder="📄"
                maxLength={2}
              />
            </div>

            {/* Metadata */}
            <div className="pt-4 border-t border-slate-200 dark:border-slate-800 space-y-2 text-sm text-slate-500">
              <p>Created: {formatDate(page.createdAt)}</p>
              <p>Updated: {formatDate(page.updatedAt)}</p>
              {page.publishedAt && (
                <p>Published: {formatDate(page.publishedAt)}</p>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="space-y-2">
            <Button
              className="w-full"
              onClick={handleSave}
              disabled={isSaving}
            >
              {isSaving ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <Save className="mr-2 h-4 w-4" />
                  Save Changes
                </>
              )}
            </Button>
            <Button
              variant="outline"
              className="w-full text-red-600 hover:text-red-700"
              onClick={handleDelete}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete Page
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
