'use client';

import { useState, useCallback } from 'react';
import Link from 'next/link';
import {
  ChevronRight,
  ChevronDown,
  File,
  Folder,
  FolderOpen,
  Plus,
  MoreHorizontal,
  Pencil,
  Trash2,
  Eye,
  EyeOff,
  GripVertical,
  FileText,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { cn } from '@/lib/utils';
import type { PageTreeNode, SpaceWithPages } from '../api/pages';

interface PageTreeProps {
  spaces: SpaceWithPages[];
  onCreatePage: (spaceId: string, parentId?: string) => void;
  onDeletePage: (pageId: string) => void;
  onPublishPage: (pageId: string) => void;
  onUnpublishPage: (pageId: string) => void;
}

interface TreeNodeProps {
  node: PageTreeNode;
  level: number;
  spaceId: string;
  onCreatePage: (spaceId: string, parentId?: string) => void;
  onDeletePage: (pageId: string) => void;
  onPublishPage: (pageId: string) => void;
  onUnpublishPage: (pageId: string) => void;
}

function TreeNode({
  node,
  level,
  spaceId,
  onCreatePage,
  onDeletePage,
  onPublishPage,
  onUnpublishPage,
}: TreeNodeProps) {
  const [isOpen, setIsOpen] = useState(level < 2);
  const hasChildren = node.children && node.children.length > 0;

  return (
    <div>
      <div
        className={cn(
          'group flex items-center gap-1 px-2 py-1.5 rounded-md hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors',
          'cursor-pointer'
        )}
        style={{ paddingLeft: `${(level * 16) + 8}px` }}
      >
        {/* Drag handle */}
        <button className="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-slate-600 cursor-grab">
          <GripVertical className="h-3.5 w-3.5" />
        </button>

        {/* Expand/collapse toggle */}
        {hasChildren ? (
          <button
            onClick={() => setIsOpen(!isOpen)}
            className="text-slate-400 hover:text-slate-600"
          >
            {isOpen ? (
              <ChevronDown className="h-4 w-4" />
            ) : (
              <ChevronRight className="h-4 w-4" />
            )}
          </button>
        ) : (
          <span className="w-4" />
        )}

        {/* Page icon */}
        {node.icon ? (
          <span className="text-sm">{node.icon}</span>
        ) : hasChildren ? (
          isOpen ? (
            <FolderOpen className="h-4 w-4 text-blue-500" />
          ) : (
            <Folder className="h-4 w-4 text-blue-500" />
          )
        ) : (
          <FileText className="h-4 w-4 text-slate-400" />
        )}

        {/* Page title */}
        <Link
          href={`/studio/pages/${node.id}`}
          className="flex-1 text-sm font-medium text-slate-700 dark:text-slate-300 hover:text-blue-600 dark:hover:text-blue-400 truncate"
        >
          {node.title}
        </Link>

        {/* Status badge */}
        {!node.isPublished && (
          <Badge variant="outline" className="text-xs py-0 h-5">
            Draft
          </Badge>
        )}

        {/* Actions */}
        <div className="opacity-0 group-hover:opacity-100 flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6"
            onClick={(e) => {
              e.stopPropagation();
              onCreatePage(spaceId, node.id);
            }}
            title="Add child page"
          >
            <Plus className="h-3.5 w-3.5" />
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-6 w-6">
                <MoreHorizontal className="h-3.5 w-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={`/studio/pages/${node.id}`}>
                  <Pencil className="mr-2 h-4 w-4" />
                  Edit
                </Link>
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => window.open(`/${node.slug}`, '_blank')}
              >
                <Eye className="mr-2 h-4 w-4" />
                View
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {node.isPublished ? (
                <DropdownMenuItem onClick={() => onUnpublishPage(node.id)}>
                  <EyeOff className="mr-2 h-4 w-4" />
                  Unpublish
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => onPublishPage(node.id)}>
                  <Eye className="mr-2 h-4 w-4" />
                  Publish
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => onDeletePage(node.id)}
                className="text-red-600 dark:text-red-400"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {/* Children */}
      {hasChildren && isOpen && (
        <div>
          {node.children.map((child) => (
            <TreeNode
              key={child.id}
              node={child}
              level={level + 1}
              spaceId={spaceId}
              onCreatePage={onCreatePage}
              onDeletePage={onDeletePage}
              onPublishPage={onPublishPage}
              onUnpublishPage={onUnpublishPage}
            />
          ))}
        </div>
      )}
    </div>
  );
}

interface SpaceSectionProps {
  space: SpaceWithPages;
  onCreatePage: (spaceId: string, parentId?: string) => void;
  onDeletePage: (pageId: string) => void;
  onPublishPage: (pageId: string) => void;
  onUnpublishPage: (pageId: string) => void;
}

function SpaceSection({
  space,
  onCreatePage,
  onDeletePage,
  onPublishPage,
  onUnpublishPage,
}: SpaceSectionProps) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <div className="mb-4">
        <CollapsibleTrigger asChild>
          <button className="flex items-center gap-2 w-full px-2 py-2 text-left hover:bg-slate-100 dark:hover:bg-slate-800 rounded-md transition-colors group">
            {isOpen ? (
              <ChevronDown className="h-4 w-4 text-slate-400" />
            ) : (
              <ChevronRight className="h-4 w-4 text-slate-400" />
            )}
            {space.iconUrl ? (
              <img src={space.iconUrl} alt="" className="h-5 w-5 rounded" />
            ) : (
              <Folder className="h-5 w-5 text-slate-400" />
            )}
            <span className="font-medium text-slate-900 dark:text-slate-100 flex-1">
              {space.name}
            </span>
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 opacity-0 group-hover:opacity-100"
              onClick={(e) => {
                e.stopPropagation();
                onCreatePage(space.id);
              }}
              title="Add page to space"
            >
              <Plus className="h-3.5 w-3.5" />
            </Button>
          </button>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className="mt-1">
            {space.pages.length === 0 ? (
              <div className="px-4 py-6 text-center text-sm text-slate-500">
                <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p>No pages yet</p>
                <Button
                  variant="link"
                  size="sm"
                  className="mt-1"
                  onClick={() => onCreatePage(space.id)}
                >
                  Create first page
                </Button>
              </div>
            ) : (
              space.pages.map((page) => (
                <TreeNode
                  key={page.id}
                  node={page}
                  level={0}
                  spaceId={space.id}
                  onCreatePage={onCreatePage}
                  onDeletePage={onDeletePage}
                  onPublishPage={onPublishPage}
                  onUnpublishPage={onUnpublishPage}
                />
              ))
            )}
          </div>
        </CollapsibleContent>
      </div>
    </Collapsible>
  );
}

export function PageTree({
  spaces,
  onCreatePage,
  onDeletePage,
  onPublishPage,
  onUnpublishPage,
}: PageTreeProps) {
  if (spaces.length === 0) {
    return (
      <div className="text-center py-12 text-slate-500">
        <Folder className="h-12 w-12 mx-auto mb-4 opacity-50" />
        <p className="text-lg font-medium mb-2">No spaces found</p>
        <p className="text-sm">Create a space first to add pages.</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {spaces.map((space) => (
        <SpaceSection
          key={space.id}
          space={space}
          onCreatePage={onCreatePage}
          onDeletePage={onDeletePage}
          onPublishPage={onPublishPage}
          onUnpublishPage={onUnpublishPage}
        />
      ))}
    </div>
  );
}
