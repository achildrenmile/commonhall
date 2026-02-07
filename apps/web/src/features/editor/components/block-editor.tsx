'use client';

import { useState, useCallback, lazy, Suspense } from 'react';
import {
  Plus,
  GripVertical,
  Trash2,
  ChevronUp,
  ChevronDown,
  Settings,
  Type,
  Image,
  AlertCircle,
  File,
  LayoutList,
  Square,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { cn } from '@/lib/utils';
import type { WidgetBlock, WidgetType } from '@/features/pages/widgets';

// Lazy load TiptapEditor to avoid SSR issues
const TiptapEditor = lazy(() =>
  import('./tiptap-editor').then((mod) => ({ default: mod.TiptapEditor }))
);

interface BlockEditorProps {
  blocks: WidgetBlock[];
  onChange: (blocks: WidgetBlock[]) => void;
}

interface BlockTypeConfig {
  type: WidgetType;
  label: string;
  icon: React.ElementType;
  defaultData: Record<string, unknown>;
}

const blockTypes: BlockTypeConfig[] = [
  {
    type: 'rich-text',
    label: 'Rich Text',
    icon: Type,
    defaultData: { html: '<p>Enter text here...</p>' },
  },
  {
    type: 'hero-image',
    label: 'Hero Image',
    icon: Image,
    defaultData: { imageUrl: '', headline: '', subheadline: '' },
  },
  {
    type: 'info-box',
    label: 'Info Box',
    icon: AlertCircle,
    defaultData: { variant: 'info', title: '', body: '' },
  },
  {
    type: 'file-list',
    label: 'File List',
    icon: File,
    defaultData: { fileIds: [], title: 'Downloads' },
  },
  {
    type: 'button',
    label: 'Button',
    icon: Square,
    defaultData: { text: 'Click me', url: '/', variant: 'default', alignment: 'left' },
  },
  {
    type: 'accordion',
    label: 'Accordion',
    icon: LayoutList,
    defaultData: { items: [{ title: 'Item 1', content: 'Content here' }] },
  },
];

function generateId(): string {
  return `block-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

// Block settings dialog components
interface BlockSettingsProps {
  block: WidgetBlock;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUpdate: (data: Record<string, unknown>) => void;
}

function HeroImageSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { imageUrl?: string; headline?: string; subheadline?: string };
  const [imageUrl, setImageUrl] = useState(data.imageUrl || '');
  const [headline, setHeadline] = useState(data.headline || '');
  const [subheadline, setSubheadline] = useState(data.subheadline || '');

  const handleSave = () => {
    onUpdate({ imageUrl, headline, subheadline });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Hero Image Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Image URL</Label>
            <Input
              value={imageUrl}
              onChange={(e) => setImageUrl(e.target.value)}
              placeholder="https://example.com/image.jpg"
            />
          </div>
          <div className="space-y-2">
            <Label>Headline</Label>
            <Input
              value={headline}
              onChange={(e) => setHeadline(e.target.value)}
              placeholder="Main headline text"
            />
          </div>
          <div className="space-y-2">
            <Label>Subheadline</Label>
            <Input
              value={subheadline}
              onChange={(e) => setSubheadline(e.target.value)}
              placeholder="Supporting text"
            />
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function InfoBoxSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { variant?: string; title?: string; body?: string };
  const [variant, setVariant] = useState(data.variant || 'info');
  const [title, setTitle] = useState(data.title || '');
  const [body, setBody] = useState(data.body || '');

  const handleSave = () => {
    onUpdate({ variant, title, body });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Info Box Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Variant</Label>
            <Select value={variant} onValueChange={setVariant}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="info">Info</SelectItem>
                <SelectItem value="warning">Warning</SelectItem>
                <SelectItem value="success">Success</SelectItem>
                <SelectItem value="error">Error</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Title</Label>
            <Input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Box title"
            />
          </div>
          <div className="space-y-2">
            <Label>Body</Label>
            <Textarea
              value={body}
              onChange={(e) => setBody(e.target.value)}
              placeholder="Box content"
              rows={3}
            />
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function ButtonSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { text?: string; url?: string; variant?: string; alignment?: string };
  const [text, setText] = useState(data.text || 'Click me');
  const [url, setUrl] = useState(data.url || '/');
  const [variant, setVariant] = useState(data.variant || 'default');
  const [alignment, setAlignment] = useState(data.alignment || 'left');

  const handleSave = () => {
    onUpdate({ text, url, variant, alignment });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Button Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Button Text</Label>
            <Input value={text} onChange={(e) => setText(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>URL</Label>
            <Input value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://..." />
          </div>
          <div className="space-y-2">
            <Label>Style</Label>
            <Select value={variant} onValueChange={setVariant}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="default">Default</SelectItem>
                <SelectItem value="secondary">Secondary</SelectItem>
                <SelectItem value="outline">Outline</SelectItem>
                <SelectItem value="ghost">Ghost</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Alignment</Label>
            <Select value={alignment} onValueChange={setAlignment}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="left">Left</SelectItem>
                <SelectItem value="center">Center</SelectItem>
                <SelectItem value="right">Right</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function BlockItem({
  block,
  index,
  totalBlocks,
  onUpdate,
  onDelete,
  onMoveUp,
  onMoveDown,
}: {
  block: WidgetBlock;
  index: number;
  totalBlocks: number;
  onUpdate: (data: Record<string, unknown>) => void;
  onDelete: () => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
}) {
  const [settingsOpen, setSettingsOpen] = useState(false);
  const config = blockTypes.find((bt) => bt.type === block.type);
  const Icon = config?.icon || Type;

  const renderSettings = () => {
    switch (block.type) {
      case 'hero-image':
        return (
          <HeroImageSettings
            block={block}
            open={settingsOpen}
            onOpenChange={setSettingsOpen}
            onUpdate={onUpdate}
          />
        );
      case 'info-box':
        return (
          <InfoBoxSettings
            block={block}
            open={settingsOpen}
            onOpenChange={setSettingsOpen}
            onUpdate={onUpdate}
          />
        );
      case 'button':
        return (
          <ButtonSettings
            block={block}
            open={settingsOpen}
            onOpenChange={setSettingsOpen}
            onUpdate={onUpdate}
          />
        );
      default:
        return null;
    }
  };

  return (
    <>
      {renderSettings()}
      <div className="group relative border border-slate-200 dark:border-slate-800 rounded-lg bg-white dark:bg-slate-950 hover:border-slate-300 dark:hover:border-slate-700 transition-colors">
        {/* Block Header */}
        <div className="flex items-center gap-2 px-3 py-2 border-b border-slate-100 dark:border-slate-800">
          <button className="cursor-grab text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
            <GripVertical className="h-4 w-4" />
          </button>
          <Icon className="h-4 w-4 text-slate-500" />
          <span className="text-sm font-medium text-slate-700 dark:text-slate-300 flex-1">
            {config?.label || block.type}
          </span>
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveUp}
              disabled={index === 0}
            >
              <ChevronUp className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={onMoveDown}
              disabled={index === totalBlocks - 1}
            >
              <ChevronDown className="h-4 w-4" />
            </Button>
            {block.type !== 'rich-text' && (
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7"
                onClick={() => setSettingsOpen(true)}
              >
                <Settings className="h-4 w-4" />
              </Button>
            )}
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-red-500 hover:text-red-600"
              onClick={onDelete}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Block Content */}
        <div className="p-4">
          {block.type === 'rich-text' && (
            <Suspense
              fallback={
                <div className="min-h-[200px] bg-slate-50 dark:bg-slate-900 rounded animate-pulse" />
              }
            >
              <TiptapEditor
                content={(block.data as { html?: string }).html || ''}
                onChange={(html) => onUpdate({ html })}
                placeholder="Start writing..."
              />
            </Suspense>
          )}
          {block.type === 'hero-image' && (
            <div
              className="text-center text-slate-500 py-8 bg-slate-50 dark:bg-slate-900 rounded cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              onClick={() => setSettingsOpen(true)}
            >
              {(block.data as { imageUrl?: string }).imageUrl ? (
                <div className="relative">
                  <img
                    src={(block.data as { imageUrl?: string }).imageUrl}
                    alt="Hero"
                    className="w-full h-48 object-cover rounded"
                  />
                  {(block.data as { headline?: string }).headline && (
                    <div className="absolute inset-0 flex flex-col items-center justify-center bg-black/40 text-white">
                      <h2 className="text-2xl font-bold">{(block.data as { headline?: string }).headline}</h2>
                      {(block.data as { subheadline?: string }).subheadline && (
                        <p className="text-lg mt-2">{(block.data as { subheadline?: string }).subheadline}</p>
                      )}
                    </div>
                  )}
                </div>
              ) : (
                <>
                  <Image className="h-8 w-8 mx-auto mb-2 opacity-50" />
                  <p className="text-sm">Hero Image Block</p>
                  <p className="text-xs text-slate-400">Click to configure</p>
                </>
              )}
            </div>
          )}
          {block.type === 'info-box' && (
            <div
              className={cn(
                'p-4 rounded cursor-pointer',
                (block.data as { variant?: string }).variant === 'warning' && 'bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800',
                (block.data as { variant?: string }).variant === 'success' && 'bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800',
                (block.data as { variant?: string }).variant === 'error' && 'bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800',
                ((block.data as { variant?: string }).variant === 'info' || !(block.data as { variant?: string }).variant) && 'bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800'
              )}
              onClick={() => setSettingsOpen(true)}
            >
              {(block.data as { title?: string }).title ? (
                <>
                  <h4 className="font-medium mb-1">{(block.data as { title?: string }).title}</h4>
                  <p className="text-sm">{(block.data as { body?: string }).body}</p>
                </>
              ) : (
                <div className="text-center text-slate-500">
                  <AlertCircle className="h-6 w-6 mx-auto mb-2 opacity-50" />
                  <p className="text-sm">Info Box - Click to configure</p>
                </div>
              )}
            </div>
          )}
          {block.type === 'file-list' && (
            <div
              className="text-center text-slate-500 py-4 bg-slate-50 dark:bg-slate-900 rounded cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              onClick={() => setSettingsOpen(true)}
            >
              <File className="h-6 w-6 mx-auto mb-2 opacity-50" />
              <p className="text-sm">File List Block</p>
              <p className="text-xs text-slate-400">Click to configure</p>
            </div>
          )}
          {block.type === 'button' && (
            <div
              className={cn(
                'py-4 cursor-pointer',
                (block.data as { alignment?: string }).alignment === 'center' && 'text-center',
                (block.data as { alignment?: string }).alignment === 'right' && 'text-right',
                (!(block.data as { alignment?: string }).alignment || (block.data as { alignment?: string }).alignment === 'left') && 'text-left'
              )}
              onClick={() => setSettingsOpen(true)}
            >
              <Button variant={(block.data as { variant?: 'default' | 'secondary' | 'outline' | 'ghost' }).variant || 'default'}>
                {(block.data as { text?: string }).text || 'Button'}
              </Button>
            </div>
          )}
          {block.type === 'accordion' && (
            <div
              className="text-center text-slate-500 py-4 bg-slate-50 dark:bg-slate-900 rounded cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              onClick={() => setSettingsOpen(true)}
            >
              <LayoutList className="h-6 w-6 mx-auto mb-2 opacity-50" />
              <p className="text-sm">Accordion Block</p>
              <p className="text-xs text-slate-400">Click to configure</p>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function AddBlockButton({ onAdd }: { onAdd: (type: WidgetType) => void }) {
  return (
    <div className="flex justify-center py-2">
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Add Block
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="center" className="w-48">
          {blockTypes.map((bt) => (
            <DropdownMenuItem key={bt.type} onClick={() => onAdd(bt.type)}>
              <bt.icon className="h-4 w-4 mr-2" />
              {bt.label}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

export function BlockEditor({ blocks, onChange }: BlockEditorProps) {
  const addBlock = useCallback(
    (type: WidgetType, index?: number) => {
      const config = blockTypes.find((bt) => bt.type === type);
      if (!config) return;

      const newBlock: WidgetBlock = {
        id: generateId(),
        type,
        data: { ...config.defaultData },
      };

      const newBlocks = [...blocks];
      if (index !== undefined) {
        newBlocks.splice(index + 1, 0, newBlock);
      } else {
        newBlocks.push(newBlock);
      }
      onChange(newBlocks);
    },
    [blocks, onChange]
  );

  const updateBlock = useCallback(
    (index: number, data: Record<string, unknown>) => {
      const newBlocks = [...blocks];
      newBlocks[index] = {
        ...newBlocks[index],
        data: { ...newBlocks[index].data, ...data },
      };
      onChange(newBlocks);
    },
    [blocks, onChange]
  );

  const deleteBlock = useCallback(
    (index: number) => {
      const newBlocks = blocks.filter((_, i) => i !== index);
      onChange(newBlocks);
    },
    [blocks, onChange]
  );

  const moveBlock = useCallback(
    (fromIndex: number, toIndex: number) => {
      if (toIndex < 0 || toIndex >= blocks.length) return;
      const newBlocks = [...blocks];
      const [removed] = newBlocks.splice(fromIndex, 1);
      newBlocks.splice(toIndex, 0, removed);
      onChange(newBlocks);
    },
    [blocks, onChange]
  );

  return (
    <div className="space-y-3">
      {blocks.length === 0 ? (
        <div className="text-center py-12 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-lg">
          <Type className="h-8 w-8 mx-auto text-slate-400 mb-3" />
          <p className="text-slate-500 mb-4">No content blocks yet</p>
          <AddBlockButton onAdd={(type) => addBlock(type)} />
        </div>
      ) : (
        <>
          {blocks.map((block, index) => (
            <div key={block.id}>
              <BlockItem
                block={block}
                index={index}
                totalBlocks={blocks.length}
                onUpdate={(data) => updateBlock(index, data)}
                onDelete={() => deleteBlock(index)}
                onMoveUp={() => moveBlock(index, index - 1)}
                onMoveDown={() => moveBlock(index, index + 1)}
              />
              {/* Add block between */}
              <div className="relative h-6 group/add">
                <div className="absolute inset-x-0 top-1/2 -translate-y-1/2 flex justify-center opacity-0 group-hover/add:opacity-100 transition-opacity">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <button className="h-6 w-6 rounded-full bg-slate-200 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 flex items-center justify-center">
                        <Plus className="h-3 w-3" />
                      </button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="center" className="w-48">
                      {blockTypes.map((bt) => (
                        <DropdownMenuItem key={bt.type} onClick={() => addBlock(bt.type, index)}>
                          <bt.icon className="h-4 w-4 mr-2" />
                          {bt.label}
                        </DropdownMenuItem>
                      ))}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            </div>
          ))}
          <AddBlockButton onAdd={(type) => addBlock(type)} />
        </>
      )}
    </div>
  );
}
