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
  Image as ImageIcon,
  Square,
  Minus,
  Layout,
  FileText,
  Heading,
  Newspaper,
  Space,
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
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';
import type { EmailBlock, EmailBlockType } from '../types';

const TiptapEditor = lazy(() =>
  import('@/features/editor/components/tiptap-editor').then((mod) => ({ default: mod.TiptapEditor }))
);

interface EmailBlockEditorProps {
  blocks: EmailBlock[];
  onChange: (blocks: EmailBlock[]) => void;
}

interface BlockTypeConfig {
  type: EmailBlockType;
  label: string;
  icon: React.ElementType;
  defaultData: Record<string, unknown>;
}

const blockTypes: BlockTypeConfig[] = [
  {
    type: 'header',
    label: 'Header',
    icon: Layout,
    defaultData: { title: '', logoUrl: '', backgroundColor: '#1e293b', textColor: '#ffffff' },
  },
  {
    type: 'heading',
    label: 'Heading',
    icon: Heading,
    defaultData: { text: '', level: 2, alignment: 'left' },
  },
  {
    type: 'text',
    label: 'Text',
    icon: Type,
    defaultData: { html: '<p>Enter text here...</p>' },
  },
  {
    type: 'image',
    label: 'Image',
    icon: ImageIcon,
    defaultData: { src: '', alt: '', width: '100%', alignment: 'center' },
  },
  {
    type: 'button',
    label: 'Button',
    icon: Square,
    defaultData: { text: 'Click Here', url: '#', alignment: 'center', backgroundColor: '#3b82f6', textColor: '#ffffff' },
  },
  {
    type: 'divider',
    label: 'Divider',
    icon: Minus,
    defaultData: {},
  },
  {
    type: 'spacer',
    label: 'Spacer',
    icon: Space,
    defaultData: { height: 24 },
  },
  {
    type: 'news-preview',
    label: 'News Preview',
    icon: Newspaper,
    defaultData: { title: '', teaser: '', imageUrl: '', linkUrl: '#' },
  },
  {
    type: 'footer',
    label: 'Footer',
    icon: FileText,
    defaultData: { text: '', showUnsubscribe: true, backgroundColor: '#f8fafc' },
  },
];

function generateId(): string {
  return `block-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
}

// Block Settings Dialogs
interface BlockSettingsProps {
  block: EmailBlock;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onUpdate: (data: Record<string, unknown>) => void;
}

function HeaderSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { title?: string; logoUrl?: string; backgroundColor?: string; textColor?: string };
  const [title, setTitle] = useState(data.title || '');
  const [logoUrl, setLogoUrl] = useState(data.logoUrl || '');
  const [backgroundColor, setBackgroundColor] = useState(data.backgroundColor || '#1e293b');
  const [textColor, setTextColor] = useState(data.textColor || '#ffffff');

  const handleSave = () => {
    onUpdate({ title, logoUrl, backgroundColor, textColor });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Header Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Logo URL</Label>
            <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://..." />
          </div>
          <div className="space-y-2">
            <Label>Title</Label>
            <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Newsletter Title" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Background Color</Label>
              <Input type="color" value={backgroundColor} onChange={(e) => setBackgroundColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Text Color</Label>
              <Input type="color" value={textColor} onChange={(e) => setTextColor(e.target.value)} />
            </div>
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function HeadingSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { text?: string; level?: number; alignment?: string };
  const [text, setText] = useState(data.text || '');
  const [level, setLevel] = useState(String(data.level || 2));
  const [alignment, setAlignment] = useState(data.alignment || 'left');

  const handleSave = () => {
    onUpdate({ text, level: parseInt(level), alignment });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Heading Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Text</Label>
            <Input value={text} onChange={(e) => setText(e.target.value)} placeholder="Heading text" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Level</Label>
              <Select value={level} onValueChange={setLevel}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">H1</SelectItem>
                  <SelectItem value="2">H2</SelectItem>
                  <SelectItem value="3">H3</SelectItem>
                  <SelectItem value="4">H4</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Alignment</Label>
              <Select value={alignment} onValueChange={setAlignment}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="left">Left</SelectItem>
                  <SelectItem value="center">Center</SelectItem>
                  <SelectItem value="right">Right</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function ImageSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { src?: string; alt?: string; width?: string; alignment?: string; linkUrl?: string };
  const [src, setSrc] = useState(data.src || '');
  const [alt, setAlt] = useState(data.alt || '');
  const [width, setWidth] = useState(data.width || '100%');
  const [alignment, setAlignment] = useState(data.alignment || 'center');
  const [linkUrl, setLinkUrl] = useState(data.linkUrl || '');

  const handleSave = () => {
    onUpdate({ src, alt, width, alignment, linkUrl: linkUrl || undefined });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Image Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Image URL</Label>
            <Input value={src} onChange={(e) => setSrc(e.target.value)} placeholder="https://..." />
          </div>
          <div className="space-y-2">
            <Label>Alt Text</Label>
            <Input value={alt} onChange={(e) => setAlt(e.target.value)} placeholder="Image description" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Width</Label>
              <Select value={width} onValueChange={setWidth}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="100%">Full Width</SelectItem>
                  <SelectItem value="75%">75%</SelectItem>
                  <SelectItem value="50%">50%</SelectItem>
                  <SelectItem value="25%">25%</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Alignment</Label>
              <Select value={alignment} onValueChange={setAlignment}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="left">Left</SelectItem>
                  <SelectItem value="center">Center</SelectItem>
                  <SelectItem value="right">Right</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="space-y-2">
            <Label>Link URL (optional)</Label>
            <Input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://..." />
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function ButtonSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { text?: string; url?: string; alignment?: string; backgroundColor?: string; textColor?: string };
  const [text, setText] = useState(data.text || 'Click Here');
  const [url, setUrl] = useState(data.url || '#');
  const [alignment, setAlignment] = useState(data.alignment || 'center');
  const [backgroundColor, setBackgroundColor] = useState(data.backgroundColor || '#3b82f6');
  const [textColor, setTextColor] = useState(data.textColor || '#ffffff');

  const handleSave = () => {
    onUpdate({ text, url, alignment, backgroundColor, textColor });
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
            <Label>Alignment</Label>
            <Select value={alignment} onValueChange={setAlignment}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value="left">Left</SelectItem>
                <SelectItem value="center">Center</SelectItem>
                <SelectItem value="right">Right</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Background</Label>
              <Input type="color" value={backgroundColor} onChange={(e) => setBackgroundColor(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label>Text Color</Label>
              <Input type="color" value={textColor} onChange={(e) => setTextColor(e.target.value)} />
            </div>
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function SpacerSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { height?: number };
  const [height, setHeight] = useState(String(data.height || 24));

  const handleSave = () => {
    onUpdate({ height: parseInt(height) });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Spacer Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Height (px)</Label>
            <Input type="number" value={height} onChange={(e) => setHeight(e.target.value)} min="8" max="200" />
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function NewsPreviewSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { title?: string; teaser?: string; imageUrl?: string; linkUrl?: string };
  const [title, setTitle] = useState(data.title || '');
  const [teaser, setTeaser] = useState(data.teaser || '');
  const [imageUrl, setImageUrl] = useState(data.imageUrl || '');
  const [linkUrl, setLinkUrl] = useState(data.linkUrl || '#');

  const handleSave = () => {
    onUpdate({ title, teaser, imageUrl: imageUrl || undefined, linkUrl });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>News Preview Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Title</Label>
            <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Article title" />
          </div>
          <div className="space-y-2">
            <Label>Teaser</Label>
            <Textarea value={teaser} onChange={(e) => setTeaser(e.target.value)} placeholder="Brief description..." rows={2} />
          </div>
          <div className="space-y-2">
            <Label>Image URL (optional)</Label>
            <Input value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} placeholder="https://..." />
          </div>
          <div className="space-y-2">
            <Label>Link URL</Label>
            <Input value={linkUrl} onChange={(e) => setLinkUrl(e.target.value)} placeholder="https://..." />
          </div>
          <Button onClick={handleSave} className="w-full">Save</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function FooterSettings({ block, open, onOpenChange, onUpdate }: BlockSettingsProps) {
  const data = block.data as { text?: string; showUnsubscribe?: boolean; backgroundColor?: string };
  const [text, setText] = useState(data.text || '');
  const [showUnsubscribe, setShowUnsubscribe] = useState(data.showUnsubscribe ?? true);
  const [backgroundColor, setBackgroundColor] = useState(data.backgroundColor || '#f8fafc');

  const handleSave = () => {
    onUpdate({ text, showUnsubscribe, backgroundColor });
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Footer Settings</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Footer Text</Label>
            <Textarea value={text} onChange={(e) => setText(e.target.value)} placeholder="Company info, address..." rows={3} />
          </div>
          <div className="flex items-center justify-between">
            <Label>Show Unsubscribe Link</Label>
            <Switch checked={showUnsubscribe} onCheckedChange={setShowUnsubscribe} />
          </div>
          <div className="space-y-2">
            <Label>Background Color</Label>
            <Input type="color" value={backgroundColor} onChange={(e) => setBackgroundColor(e.target.value)} />
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
  block: EmailBlock;
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
    const props = { block, open: settingsOpen, onOpenChange: setSettingsOpen, onUpdate };
    switch (block.type) {
      case 'header': return <HeaderSettings {...props} />;
      case 'heading': return <HeadingSettings {...props} />;
      case 'image': return <ImageSettings {...props} />;
      case 'button': return <ButtonSettings {...props} />;
      case 'spacer': return <SpacerSettings {...props} />;
      case 'news-preview': return <NewsPreviewSettings {...props} />;
      case 'footer': return <FooterSettings {...props} />;
      default: return null;
    }
  };

  const hasSettings = !['text', 'divider'].includes(block.type);

  return (
    <>
      {renderSettings()}
      <div className="group relative border border-slate-200 dark:border-slate-800 rounded-lg bg-white dark:bg-slate-950 hover:border-slate-300 dark:hover:border-slate-700 transition-colors">
        <div className="flex items-center gap-2 px-3 py-2 border-b border-slate-100 dark:border-slate-800">
          <button className="cursor-grab text-slate-400 hover:text-slate-600 dark:hover:text-slate-300">
            <GripVertical className="h-4 w-4" />
          </button>
          <Icon className="h-4 w-4 text-slate-500" />
          <span className="text-sm font-medium text-slate-700 dark:text-slate-300 flex-1">
            {config?.label || block.type}
          </span>
          <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <Button variant="ghost" size="icon" className="h-7 w-7" onClick={onMoveUp} disabled={index === 0}>
              <ChevronUp className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="icon" className="h-7 w-7" onClick={onMoveDown} disabled={index === totalBlocks - 1}>
              <ChevronDown className="h-4 w-4" />
            </Button>
            {hasSettings && (
              <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => setSettingsOpen(true)}>
                <Settings className="h-4 w-4" />
              </Button>
            )}
            <Button variant="ghost" size="icon" className="h-7 w-7 text-red-500 hover:text-red-600" onClick={onDelete}>
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </div>

        <div className="p-4">
          {block.type === 'text' && (
            <Suspense fallback={<div className="min-h-[100px] bg-slate-50 dark:bg-slate-900 rounded animate-pulse" />}>
              <TiptapEditor
                content={(block.data as { html?: string }).html || ''}
                onChange={(html) => onUpdate({ html })}
                placeholder="Enter text..."
              />
            </Suspense>
          )}
          {block.type === 'header' && (
            <div
              className="text-center p-4 rounded cursor-pointer"
              style={{ backgroundColor: (block.data as { backgroundColor?: string }).backgroundColor || '#1e293b' }}
              onClick={() => setSettingsOpen(true)}
            >
              <p style={{ color: (block.data as { textColor?: string }).textColor || '#fff' }} className="font-bold">
                {(block.data as { title?: string }).title || 'Header - Click to configure'}
              </p>
            </div>
          )}
          {block.type === 'heading' && (
            <div className="cursor-pointer" onClick={() => setSettingsOpen(true)}>
              <p className="font-bold text-slate-700 dark:text-slate-300">
                {(block.data as { text?: string }).text || 'Heading - Click to configure'}
              </p>
            </div>
          )}
          {block.type === 'image' && (
            <div className="text-center cursor-pointer" onClick={() => setSettingsOpen(true)}>
              {(block.data as { src?: string }).src ? (
                <img src={(block.data as { src?: string }).src} alt="" className="max-h-48 mx-auto rounded" />
              ) : (
                <div className="py-8 bg-slate-50 dark:bg-slate-900 rounded">
                  <ImageIcon className="h-8 w-8 mx-auto text-slate-400 mb-2" />
                  <p className="text-sm text-slate-500">Image - Click to configure</p>
                </div>
              )}
            </div>
          )}
          {block.type === 'button' && (
            <div className={cn('cursor-pointer', `text-${(block.data as { alignment?: string }).alignment || 'center'}`)} onClick={() => setSettingsOpen(true)}>
              <button
                className="px-6 py-2 rounded font-medium"
                style={{
                  backgroundColor: (block.data as { backgroundColor?: string }).backgroundColor || '#3b82f6',
                  color: (block.data as { textColor?: string }).textColor || '#fff',
                }}
              >
                {(block.data as { text?: string }).text || 'Button'}
              </button>
            </div>
          )}
          {block.type === 'divider' && (
            <hr className="border-slate-200 dark:border-slate-700" />
          )}
          {block.type === 'spacer' && (
            <div
              className="bg-slate-50 dark:bg-slate-900 rounded flex items-center justify-center text-slate-400 text-xs cursor-pointer"
              style={{ height: (block.data as { height?: number }).height || 24 }}
              onClick={() => setSettingsOpen(true)}
            >
              {(block.data as { height?: number }).height || 24}px spacer
            </div>
          )}
          {block.type === 'news-preview' && (
            <div className="p-4 bg-slate-50 dark:bg-slate-900 rounded cursor-pointer" onClick={() => setSettingsOpen(true)}>
              <h4 className="font-medium mb-1">{(block.data as { title?: string }).title || 'News Title'}</h4>
              <p className="text-sm text-slate-500">{(block.data as { teaser?: string }).teaser || 'News teaser text...'}</p>
            </div>
          )}
          {block.type === 'footer' && (
            <div
              className="p-4 rounded text-center text-sm text-slate-500 cursor-pointer"
              style={{ backgroundColor: (block.data as { backgroundColor?: string }).backgroundColor || '#f8fafc' }}
              onClick={() => setSettingsOpen(true)}
            >
              {(block.data as { text?: string }).text || 'Footer - Click to configure'}
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function AddBlockButton({ onAdd }: { onAdd: (type: EmailBlockType) => void }) {
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

export function EmailBlockEditor({ blocks, onChange }: EmailBlockEditorProps) {
  const addBlock = useCallback(
    (type: EmailBlockType, index?: number) => {
      const config = blockTypes.find((bt) => bt.type === type);
      if (!config) return;

      const newBlock: EmailBlock = {
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
      newBlocks[index] = { ...newBlocks[index], data: { ...newBlocks[index].data, ...data } };
      onChange(newBlocks);
    },
    [blocks, onChange]
  );

  const deleteBlock = useCallback(
    (index: number) => {
      onChange(blocks.filter((_, i) => i !== index));
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
          <p className="text-slate-500 mb-4">No email blocks yet</p>
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
