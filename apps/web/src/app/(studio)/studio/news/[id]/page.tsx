'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Save,
  Send,
  Clock,
  Eye,
  Trash2,
  Loader2,
  Image as ImageIcon,
  X,
  Check,
  ChevronDown,
  Calendar as CalendarIcon,
  User,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Calendar } from '@/components/ui/calendar';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import { cn, formatDate } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';
import { useAutoSave } from '@/lib/hooks/use-auto-save';
import { BlockEditor } from '@/features/editor';
import { AutoSaveIndicator } from '@/features/studio/components/auto-save-indicator';
import type { WidgetBlock } from '@/features/pages/widgets';
import {
  useStudioNewsArticle,
  useCreateArticle,
  useUpdateArticle,
  usePublishArticle,
  useScheduleArticle,
  useDeleteArticle,
  useStudioChannels,
  useStudioSpaces,
  useTagsAutocomplete,
  useUserSearch,
  type StudioNewsArticle,
  type UserSearchResult,
} from '@/features/studio/api/news';

type PublishMode = 'now' | 'schedule';

interface ArticleFormData {
  title: string;
  teaserText: string;
  teaserImageUrl: string;
  content: WidgetBlock[];
  channelId: string;
  spaceId: string;
  displayAuthorId: string;
  tags: Array<{ id: string; name: string; slug: string }>;
  isPinned: boolean;
  allowComments: boolean;
}

const defaultFormData: ArticleFormData = {
  title: '',
  teaserText: '',
  teaserImageUrl: '',
  content: [],
  channelId: '',
  spaceId: '',
  displayAuthorId: '',
  tags: [],
  isPinned: false,
  allowComments: true,
};

function TagInput({
  tags,
  onChange,
}: {
  tags: Array<{ id: string; name: string; slug: string }>;
  onChange: (tags: Array<{ id: string; name: string; slug: string }>) => void;
}) {
  const [inputValue, setInputValue] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const debouncedSearch = useDebounce(inputValue, 300);
  const { data: suggestions = [] } = useTagsAutocomplete(debouncedSearch);

  const addTag = (tag: { id: string; name: string; slug: string }) => {
    if (!tags.find((t) => t.id === tag.id)) {
      onChange([...tags, tag]);
    }
    setInputValue('');
    setIsOpen(false);
  };

  const removeTag = (tagId: string) => {
    onChange(tags.filter((t) => t.id !== tagId));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && inputValue.trim()) {
      e.preventDefault();
      // Create a new tag if no suggestions match
      const existing = suggestions.find(
        (s) => s.name.toLowerCase() === inputValue.toLowerCase()
      );
      if (existing) {
        addTag(existing);
      } else {
        const slug = inputValue.toLowerCase().replace(/\s+/g, '-');
        addTag({ id: `new-${slug}`, name: inputValue.trim(), slug });
      }
    }
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-1">
        {tags.map((tag) => (
          <Badge key={tag.id} variant="secondary" className="gap-1">
            {tag.name}
            <button onClick={() => removeTag(tag.id)}>
              <X className="h-3 w-3" />
            </button>
          </Badge>
        ))}
      </div>
      <Popover open={isOpen && suggestions.length > 0} onOpenChange={setIsOpen}>
        <PopoverTrigger asChild>
          <Input
            placeholder="Add tags..."
            value={inputValue}
            onChange={(e) => {
              setInputValue(e.target.value);
              setIsOpen(true);
            }}
            onKeyDown={handleKeyDown}
            onFocus={() => setIsOpen(true)}
          />
        </PopoverTrigger>
        <PopoverContent className="p-0 w-[200px]" align="start">
          <Command>
            <CommandList>
              <CommandGroup>
                {suggestions
                  .filter((s) => !tags.find((t) => t.id === s.id))
                  .map((suggestion) => (
                    <CommandItem
                      key={suggestion.id}
                      onSelect={() => addTag(suggestion)}
                    >
                      {suggestion.name}
                    </CommandItem>
                  ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
}

function UserPicker({
  selectedUser,
  onSelect,
  placeholder = 'Select author...',
}: {
  selectedUser?: UserSearchResult | null;
  onSelect: (user: UserSearchResult | null) => void;
  placeholder?: string;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 300);
  const { data: users = [] } = useUserSearch(debouncedSearch);

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          className="w-full justify-between"
        >
          {selectedUser ? (
            <div className="flex items-center gap-2">
              <Avatar className="h-5 w-5">
                <AvatarImage src={selectedUser.avatarUrl} />
                <AvatarFallback className="text-xs">
                  {selectedUser.displayName?.[0] || selectedUser.firstName?.[0] || '?'}
                </AvatarFallback>
              </Avatar>
              <span className="truncate">{selectedUser.displayName}</span>
            </div>
          ) : (
            <span className="text-slate-500">{placeholder}</span>
          )}
          <ChevronDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="p-0 w-[280px]" align="start">
        <Command>
          <CommandInput
            placeholder="Search users..."
            value={search}
            onValueChange={setSearch}
          />
          <CommandList>
            <CommandEmpty>No users found.</CommandEmpty>
            <CommandGroup>
              {selectedUser && (
                <CommandItem onSelect={() => { onSelect(null); setOpen(false); }}>
                  <X className="mr-2 h-4 w-4" />
                  Clear selection
                </CommandItem>
              )}
              {users.map((user) => (
                <CommandItem
                  key={user.id}
                  onSelect={() => {
                    onSelect(user);
                    setOpen(false);
                    setSearch('');
                  }}
                >
                  <Avatar className="h-6 w-6 mr-2">
                    <AvatarImage src={user.avatarUrl} />
                    <AvatarFallback className="text-xs">
                      {user.displayName?.[0] || user.firstName?.[0] || '?'}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex flex-col">
                    <span>{user.displayName}</span>
                    <span className="text-xs text-slate-500">{user.email}</span>
                  </div>
                  {selectedUser?.id === user.id && (
                    <Check className="ml-auto h-4 w-4" />
                  )}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

function EditorSkeleton() {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-[1fr,380px] gap-6">
      <div className="space-y-6">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-48 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
      <div className="space-y-6">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-32 w-full" />
        <Skeleton className="h-10 w-full" />
      </div>
    </div>
  );
}

export default function NewsEditorPage() {
  const params = useParams();
  const router = useRouter();
  const articleId = params.id as string;
  const isNewArticle = articleId === 'new';

  const { data: article, isLoading: articleLoading } = useStudioNewsArticle(articleId);
  const { data: channels = [] } = useStudioChannels();
  const { data: spaces = [] } = useStudioSpaces();

  const createArticle = useCreateArticle();
  const updateArticle = useUpdateArticle();
  const publishArticle = usePublishArticle();
  const scheduleArticle = useScheduleArticle();
  const deleteArticle = useDeleteArticle();

  const [formData, setFormData] = useState<ArticleFormData>(defaultFormData);
  const [publishMode, setPublishMode] = useState<PublishMode>('now');
  const [scheduleDate, setScheduleDate] = useState<Date | undefined>();
  const [scheduleTime, setScheduleTime] = useState('09:00');
  const [selectedDisplayAuthor, setSelectedDisplayAuthor] = useState<UserSearchResult | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [savedArticleId, setSavedArticleId] = useState<string | null>(null);

  // Auto-save for existing articles
  const handleAutoSave = useCallback(async (data: ArticleFormData) => {
    const id = savedArticleId || articleId;
    if (!id || id === 'new') return;

    const payload = {
      ...data,
      displayAuthorId: selectedDisplayAuthor?.id || undefined,
      tags: data.tags.map((t) => t.name),
    };

    await updateArticle.mutateAsync({ id, ...payload });
  }, [articleId, savedArticleId, selectedDisplayAuthor, updateArticle]);

  const autoSave = useAutoSave({
    data: formData,
    onSave: handleAutoSave,
    interval: 30000, // 30 seconds
    debounce: 2000, // 2 seconds after typing stops
    enabled: !isNewArticle && !!article, // Only for existing articles
  });

  // Load article data
  useEffect(() => {
    if (article) {
      setFormData({
        title: article.title || '',
        teaserText: article.teaserText || '',
        teaserImageUrl: article.teaserImageUrl || '',
        content: article.content || [],
        channelId: article.channelId || '',
        spaceId: article.spaceId || '',
        displayAuthorId: article.displayAuthorId || '',
        tags: article.tags || [],
        isPinned: article.isPinned,
        allowComments: article.allowComments,
      });
      if (article.displayAuthor) {
        setSelectedDisplayAuthor({
          id: article.displayAuthor.id,
          displayName: article.displayAuthor.displayName,
          firstName: article.displayAuthor.firstName,
          lastName: article.displayAuthor.lastName,
          avatarUrl: article.displayAuthor.avatarUrl,
          email: '',
        });
      }
      if (article.scheduledAt) {
        const scheduledDate = new Date(article.scheduledAt);
        setScheduleDate(scheduledDate);
        setScheduleTime(
          `${scheduledDate.getHours().toString().padStart(2, '0')}:${scheduledDate.getMinutes().toString().padStart(2, '0')}`
        );
        setPublishMode('schedule');
      }
    }
  }, [article]);

  const updateField = useCallback(
    <K extends keyof ArticleFormData>(field: K, value: ArticleFormData[K]) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
    },
    []
  );

  const handleSaveDraft = async () => {
    setIsSaving(true);
    try {
      const payload = {
        ...formData,
        displayAuthorId: selectedDisplayAuthor?.id || undefined,
        tags: formData.tags.map((t) => t.name),
      };

      if (isNewArticle) {
        const result = await createArticle.mutateAsync(payload);
        setSavedArticleId(result.id);
        router.replace(`/studio/news/${result.id}`);
      } else {
        await updateArticle.mutateAsync({ id: articleId, ...payload });
      }
    } catch (error) {
      console.error('Failed to save:', error);
    } finally {
      setIsSaving(false);
    }
  };

  const handlePublish = async () => {
    setIsSaving(true);
    try {
      // Save first
      const payload = {
        ...formData,
        displayAuthorId: selectedDisplayAuthor?.id || undefined,
        tags: formData.tags.map((t) => t.name),
      };

      let id = articleId;
      if (isNewArticle) {
        const result = await createArticle.mutateAsync(payload);
        id = result.id;
      } else {
        await updateArticle.mutateAsync({ id: articleId, ...payload });
      }

      if (publishMode === 'schedule' && scheduleDate) {
        const scheduledAt = new Date(scheduleDate);
        const [hours, minutes] = scheduleTime.split(':').map(Number);
        scheduledAt.setHours(hours, minutes, 0, 0);
        await scheduleArticle.mutateAsync({ id, scheduledAt: scheduledAt.toISOString() });
      } else {
        await publishArticle.mutateAsync(id);
      }

      router.push('/studio/news');
    } catch (error) {
      console.error('Failed to publish:', error);
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this article? This cannot be undone.')) {
      return;
    }
    try {
      await deleteArticle.mutateAsync(articleId);
      router.push('/studio/news');
    } catch (error) {
      console.error('Failed to delete:', error);
    }
  };

  const handlePreview = () => {
    // Open preview in new tab
    if (!isNewArticle) {
      window.open(`/news/preview/${articleId}`, '_blank');
    }
  };

  if (!isNewArticle && articleLoading) {
    return (
      <div className="max-w-7xl mx-auto">
        <EditorSkeleton />
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link href="/studio/news">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
              {isNewArticle ? 'New Article' : 'Edit Article'}
            </h1>
            {!isNewArticle && (
              <AutoSaveIndicator
                status={autoSave.status}
                lastSavedAt={autoSave.lastSavedAt}
                isDirty={autoSave.isDirty}
              />
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          {!isNewArticle && article?.status === 'Published' && (
            <Badge className="bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
              Published
            </Badge>
          )}
          {!isNewArticle && article?.status === 'Scheduled' && (
            <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
              Scheduled
            </Badge>
          )}
        </div>
      </div>

      {/* Editor Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-[1fr,380px] gap-6">
        {/* Left Panel - Content */}
        <div className="space-y-6">
          {/* Title */}
          <div>
            <Input
              placeholder="Article title..."
              value={formData.title}
              onChange={(e) => updateField('title', e.target.value)}
              className="text-2xl font-bold border-0 px-0 focus-visible:ring-0 placeholder:text-slate-400"
            />
          </div>

          {/* Teaser Text */}
          <div>
            <Label className="text-sm text-slate-500 mb-2 block">Teaser</Label>
            <Textarea
              placeholder="Write a short teaser..."
              value={formData.teaserText}
              onChange={(e) => updateField('teaserText', e.target.value)}
              className="resize-none"
              rows={3}
            />
          </div>

          {/* Teaser Image */}
          <div>
            <Label className="text-sm text-slate-500 mb-2 block">Teaser Image</Label>
            {formData.teaserImageUrl ? (
              <div className="relative rounded-lg overflow-hidden border border-slate-200 dark:border-slate-800">
                <img
                  src={formData.teaserImageUrl}
                  alt="Teaser"
                  className="w-full h-48 object-cover"
                />
                <Button
                  variant="secondary"
                  size="icon"
                  className="absolute top-2 right-2"
                  onClick={() => updateField('teaserImageUrl', '')}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ) : (
              <button
                onClick={() => {
                  // TODO: Open file picker modal
                  const url = prompt('Enter image URL:');
                  if (url) updateField('teaserImageUrl', url);
                }}
                className="w-full h-32 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-lg flex flex-col items-center justify-center gap-2 text-slate-500 hover:border-slate-300 dark:hover:border-slate-700 transition-colors"
              >
                <ImageIcon className="h-8 w-8" />
                <span className="text-sm">Click to add teaser image</span>
              </button>
            )}
          </div>

          {/* Content Block Editor */}
          <div>
            <Label className="text-sm text-slate-500 mb-2 block">Content</Label>
            <BlockEditor
              blocks={formData.content}
              onChange={(blocks) => updateField('content', blocks)}
            />
          </div>
        </div>

        {/* Right Panel - Settings */}
        <div className="space-y-6">
          <div className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-4 space-y-6">
            {/* Status */}
            {!isNewArticle && article && (
              <div>
                <Label className="text-sm text-slate-500 mb-2 block">Status</Label>
                <Badge className={cn(
                  article.status === 'Draft' && 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
                  article.status === 'Published' && 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
                  article.status === 'Scheduled' && 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
                  article.status === 'Archived' && 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
                )}>
                  {article.status}
                </Badge>
              </div>
            )}

            {/* Space */}
            <div>
              <Label className="text-sm text-slate-500 mb-2 block">Space</Label>
              <Select
                value={formData.spaceId}
                onValueChange={(value) => updateField('spaceId', value)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select space..." />
                </SelectTrigger>
                <SelectContent>
                  {(spaces as Array<{ id: string; name: string; slug: string }>).map((space) => (
                    <SelectItem key={space.id} value={space.id}>
                      {space.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Channel */}
            <div>
              <Label className="text-sm text-slate-500 mb-2 block">Channel</Label>
              <Select
                value={formData.channelId}
                onValueChange={(value) => updateField('channelId', value)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select channel..." />
                </SelectTrigger>
                <SelectContent>
                  {channels.map((channel) => (
                    <SelectItem key={channel.id} value={channel.id}>
                      {channel.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Tags */}
            <div>
              <Label className="text-sm text-slate-500 mb-2 block">Tags</Label>
              <TagInput
                tags={formData.tags}
                onChange={(tags) => updateField('tags', tags)}
              />
            </div>

            {/* Display Author (Ghostwriting) */}
            <div>
              <Label className="text-sm text-slate-500 mb-2 block">
                Post As (Ghostwriting)
              </Label>
              <UserPicker
                selectedUser={selectedDisplayAuthor}
                onSelect={setSelectedDisplayAuthor}
                placeholder="Select author..."
              />
              <p className="text-xs text-slate-500 mt-1">
                Leave empty to post as yourself
              </p>
            </div>

            {/* Allow Comments */}
            <div className="flex items-center justify-between">
              <Label className="text-sm">Allow Comments</Label>
              <Switch
                checked={formData.allowComments}
                onCheckedChange={(checked) => updateField('allowComments', checked)}
              />
            </div>

            {/* Pin Article */}
            <div className="flex items-center justify-between">
              <Label className="text-sm">Pin to Top</Label>
              <Switch
                checked={formData.isPinned}
                onCheckedChange={(checked) => updateField('isPinned', checked)}
              />
            </div>

            {/* Publish Options */}
            <div className="pt-4 border-t border-slate-200 dark:border-slate-800">
              <Label className="text-sm text-slate-500 mb-3 block">
                Publish Options
              </Label>
              <div className="space-y-3">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="publishMode"
                    checked={publishMode === 'now'}
                    onChange={() => setPublishMode('now')}
                    className="text-slate-900"
                  />
                  <span className="text-sm">Publish immediately</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="publishMode"
                    checked={publishMode === 'schedule'}
                    onChange={() => setPublishMode('schedule')}
                    className="text-slate-900"
                  />
                  <span className="text-sm">Schedule for later</span>
                </label>
              </div>

              {publishMode === 'schedule' && (
                <div className="mt-4 space-y-3">
                  <Popover>
                    <PopoverTrigger asChild>
                      <Button variant="outline" className="w-full justify-start">
                        <CalendarIcon className="mr-2 h-4 w-4" />
                        {scheduleDate ? formatDate(scheduleDate.toISOString()) : 'Pick a date'}
                      </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="start">
                      <Calendar
                        selected={scheduleDate}
                        onSelect={setScheduleDate}
                        minDate={new Date()}
                      />
                    </PopoverContent>
                  </Popover>
                  <Input
                    type="time"
                    value={scheduleTime}
                    onChange={(e) => setScheduleTime(e.target.value)}
                  />
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="space-y-2">
            <Button
              className="w-full"
              onClick={handlePublish}
              disabled={isSaving || !formData.title}
            >
              {isSaving ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {publishMode === 'schedule' ? 'Scheduling...' : 'Publishing...'}
                </>
              ) : (
                <>
                  {publishMode === 'schedule' ? (
                    <>
                      <Clock className="mr-2 h-4 w-4" />
                      Schedule
                    </>
                  ) : (
                    <>
                      <Send className="mr-2 h-4 w-4" />
                      Publish
                    </>
                  )}
                </>
              )}
            </Button>
            <Button
              variant="outline"
              className="w-full"
              onClick={handleSaveDraft}
              disabled={isSaving}
            >
              {isSaving ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              Save Draft
            </Button>
            {!isNewArticle && (
              <>
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={handlePreview}
                >
                  <Eye className="mr-2 h-4 w-4" />
                  Preview
                </Button>
                <Button
                  variant="outline"
                  className="w-full text-red-600 hover:text-red-700"
                  onClick={handleDelete}
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete
                </Button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
