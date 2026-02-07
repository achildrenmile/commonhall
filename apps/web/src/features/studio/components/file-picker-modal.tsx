'use client';

import { useState, useCallback, useMemo } from 'react';
import {
  Search,
  Upload,
  Folder,
  Image as ImageIcon,
  File,
  FileText,
  Video,
  Music,
  Loader2,
  Check,
  X,
  Grid,
  List,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Skeleton } from '@/components/ui/skeleton';
import { cn, formatFileSize } from '@/lib/utils';
import { useDebounce } from '@/lib/hooks/use-debounce';
import {
  useFilesList,
  useUploadFile,
  useFileCollections,
  type StudioFile,
  type FilesListFilters,
} from '../api/files';

interface FilePickerModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSelect: (file: StudioFile) => void;
  accept?: 'image' | 'document' | 'video' | 'audio' | 'all';
  title?: string;
}

function getFileIcon(mimeType: string) {
  if (mimeType.startsWith('image/')) return ImageIcon;
  if (mimeType.startsWith('video/')) return Video;
  if (mimeType.startsWith('audio/')) return Music;
  if (mimeType.includes('pdf') || mimeType.includes('document')) return FileText;
  return File;
}

function FileCard({
  file,
  isSelected,
  onSelect,
  view,
}: {
  file: StudioFile;
  isSelected: boolean;
  onSelect: () => void;
  view: 'grid' | 'list';
}) {
  const Icon = getFileIcon(file.mimeType);
  const isImage = file.mimeType.startsWith('image/');

  if (view === 'list') {
    return (
      <button
        onClick={onSelect}
        className={cn(
          'flex items-center gap-3 w-full p-3 rounded-lg text-left transition-colors',
          'hover:bg-slate-100 dark:hover:bg-slate-800',
          isSelected && 'bg-blue-50 dark:bg-blue-900/20 ring-2 ring-blue-500'
        )}
      >
        {isImage && file.thumbnailUrl ? (
          <img
            src={file.thumbnailUrl}
            alt={file.name}
            className="h-10 w-10 object-cover rounded"
          />
        ) : (
          <div className="h-10 w-10 flex items-center justify-center bg-slate-100 dark:bg-slate-800 rounded">
            <Icon className="h-5 w-5 text-slate-500" />
          </div>
        )}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">{file.originalName}</p>
          <p className="text-xs text-slate-500">{formatFileSize(file.size)}</p>
        </div>
        {isSelected && (
          <Check className="h-5 w-5 text-blue-500 shrink-0" />
        )}
      </button>
    );
  }

  return (
    <button
      onClick={onSelect}
      className={cn(
        'relative group rounded-lg overflow-hidden border transition-all',
        'hover:border-slate-300 dark:hover:border-slate-600',
        isSelected
          ? 'border-blue-500 ring-2 ring-blue-500'
          : 'border-slate-200 dark:border-slate-800'
      )}
    >
      {isImage && file.thumbnailUrl ? (
        <div className="aspect-square">
          <img
            src={file.thumbnailUrl}
            alt={file.name}
            className="w-full h-full object-cover"
          />
        </div>
      ) : (
        <div className="aspect-square flex flex-col items-center justify-center bg-slate-50 dark:bg-slate-900">
          <Icon className="h-10 w-10 text-slate-400 mb-2" />
          <span className="text-xs text-slate-500 uppercase">
            {file.mimeType.split('/')[1]?.substring(0, 4) || 'file'}
          </span>
        </div>
      )}
      <div className="p-2 bg-white dark:bg-slate-950">
        <p className="text-xs font-medium truncate">{file.originalName}</p>
        <p className="text-xs text-slate-500">{formatFileSize(file.size)}</p>
      </div>
      {isSelected && (
        <div className="absolute top-2 right-2 h-6 w-6 bg-blue-500 rounded-full flex items-center justify-center">
          <Check className="h-4 w-4 text-white" />
        </div>
      )}
    </button>
  );
}

function UploadTab({ onUploadComplete }: { onUploadComplete: (file: StudioFile) => void }) {
  const [isDragging, setIsDragging] = useState(false);
  const uploadFile = useUploadFile();

  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      setIsDragging(false);

      const files = Array.from(e.dataTransfer.files);
      if (files.length > 0) {
        const result = await uploadFile.mutateAsync(files[0]);
        onUploadComplete(result);
      }
    },
    [uploadFile, onUploadComplete]
  );

  const handleFileInput = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = e.target.files;
      if (files && files.length > 0) {
        const result = await uploadFile.mutateAsync(files[0]);
        onUploadComplete(result);
      }
    },
    [uploadFile, onUploadComplete]
  );

  return (
    <div className="p-6">
      <div
        className={cn(
          'border-2 border-dashed rounded-lg p-12 text-center transition-colors',
          isDragging
            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
            : 'border-slate-200 dark:border-slate-800',
          uploadFile.isPending && 'pointer-events-none opacity-50'
        )}
        onDragOver={(e) => {
          e.preventDefault();
          setIsDragging(true);
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
      >
        {uploadFile.isPending ? (
          <>
            <Loader2 className="h-12 w-12 mx-auto text-slate-400 mb-4 animate-spin" />
            <p className="text-lg font-medium mb-2">Uploading...</p>
          </>
        ) : (
          <>
            <Upload className="h-12 w-12 mx-auto text-slate-400 mb-4" />
            <p className="text-lg font-medium mb-2">Drop files here</p>
            <p className="text-sm text-slate-500 mb-4">or click to browse</p>
            <input
              type="file"
              id="file-upload"
              className="hidden"
              onChange={handleFileInput}
            />
            <Button asChild variant="outline">
              <label htmlFor="file-upload" className="cursor-pointer">
                Choose File
              </label>
            </Button>
          </>
        )}
      </div>
    </div>
  );
}

function BrowseTab({
  accept,
  selectedFile,
  onSelect,
}: {
  accept?: 'image' | 'document' | 'video' | 'audio' | 'all';
  selectedFile: StudioFile | null;
  onSelect: (file: StudioFile) => void;
}) {
  const [searchInput, setSearchInput] = useState('');
  const [typeFilter, setTypeFilter] = useState<FilesListFilters['type']>(accept || 'all');
  const [view, setView] = useState<'grid' | 'list'>('grid');

  const debouncedSearch = useDebounce(searchInput, 300);

  const filters: FilesListFilters = {
    type: typeFilter,
    search: debouncedSearch || undefined,
  };

  const { data, isLoading, isFetchingNextPage, hasNextPage, fetchNextPage } = useFilesList(filters);

  const files = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  return (
    <div className="flex flex-col h-full">
      {/* Filters */}
      <div className="flex items-center gap-3 p-4 border-b border-slate-200 dark:border-slate-800">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Search files..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="pl-9"
          />
        </div>
        <Select
          value={typeFilter}
          onValueChange={(v) => setTypeFilter(v as FilesListFilters['type'])}
        >
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Files</SelectItem>
            <SelectItem value="image">Images</SelectItem>
            <SelectItem value="document">Documents</SelectItem>
            <SelectItem value="video">Videos</SelectItem>
            <SelectItem value="audio">Audio</SelectItem>
          </SelectContent>
        </Select>
        <div className="flex border border-slate-200 dark:border-slate-800 rounded-md">
          <Button
            variant="ghost"
            size="icon"
            className={cn('h-8 w-8 rounded-none rounded-l-md', view === 'grid' && 'bg-slate-100 dark:bg-slate-800')}
            onClick={() => setView('grid')}
          >
            <Grid className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className={cn('h-8 w-8 rounded-none rounded-r-md', view === 'list' && 'bg-slate-100 dark:bg-slate-800')}
            onClick={() => setView('list')}
          >
            <List className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* File Grid/List */}
      <ScrollArea className="flex-1">
        <div className="p-4">
          {isLoading ? (
            <div className={cn(
              view === 'grid' ? 'grid grid-cols-4 gap-4' : 'space-y-2'
            )}>
              {Array.from({ length: 8 }).map((_, i) => (
                <Skeleton
                  key={i}
                  className={view === 'grid' ? 'aspect-square rounded-lg' : 'h-16 rounded-lg'}
                />
              ))}
            </div>
          ) : files.length === 0 ? (
            <div className="text-center py-12 text-slate-500">
              <Folder className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No files found</p>
            </div>
          ) : (
            <>
              <div className={cn(
                view === 'grid' ? 'grid grid-cols-4 gap-4' : 'space-y-2'
              )}>
                {files.map((file) => (
                  <FileCard
                    key={file.id}
                    file={file}
                    isSelected={selectedFile?.id === file.id}
                    onSelect={() => onSelect(file)}
                    view={view}
                  />
                ))}
              </div>
              {hasNextPage && (
                <div className="mt-4 text-center">
                  <Button
                    variant="outline"
                    onClick={() => fetchNextPage()}
                    disabled={isFetchingNextPage}
                  >
                    {isFetchingNextPage ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Loading...
                      </>
                    ) : (
                      'Load more'
                    )}
                  </Button>
                </div>
              )}
            </>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

export function FilePickerModal({
  open,
  onOpenChange,
  onSelect,
  accept = 'all',
  title = 'Select File',
}: FilePickerModalProps) {
  const [selectedFile, setSelectedFile] = useState<StudioFile | null>(null);
  const [activeTab, setActiveTab] = useState<'browse' | 'upload'>('browse');

  const handleSelect = () => {
    if (selectedFile) {
      onSelect(selectedFile);
      onOpenChange(false);
      setSelectedFile(null);
    }
  };

  const handleUploadComplete = (file: StudioFile) => {
    setSelectedFile(file);
    setActiveTab('browse');
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl h-[80vh] flex flex-col p-0">
        <DialogHeader className="px-6 py-4 border-b border-slate-200 dark:border-slate-800">
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <Tabs
          value={activeTab}
          onValueChange={(v) => setActiveTab(v as 'browse' | 'upload')}
          className="flex-1 flex flex-col min-h-0"
        >
          <TabsList className="mx-6 mt-4 w-fit">
            <TabsTrigger value="browse">Browse</TabsTrigger>
            <TabsTrigger value="upload">Upload</TabsTrigger>
          </TabsList>

          <TabsContent value="browse" className="flex-1 m-0 min-h-0">
            <BrowseTab
              accept={accept}
              selectedFile={selectedFile}
              onSelect={setSelectedFile}
            />
          </TabsContent>

          <TabsContent value="upload" className="flex-1 m-0">
            <UploadTab onUploadComplete={handleUploadComplete} />
          </TabsContent>
        </Tabs>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900">
          <div>
            {selectedFile && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-slate-500">Selected:</span>
                <span className="font-medium">{selectedFile.originalName}</span>
                <button
                  onClick={() => setSelectedFile(null)}
                  className="text-slate-400 hover:text-slate-600"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button onClick={handleSelect} disabled={!selectedFile}>
              Select
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
