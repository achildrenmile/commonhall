'use client';

import { Check, Loader2, AlertCircle, Cloud, CloudOff } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { AutoSaveStatus } from '@/lib/hooks/use-auto-save';

interface AutoSaveIndicatorProps {
  status: AutoSaveStatus;
  lastSavedAt: Date | null;
  isDirty: boolean;
  className?: string;
}

function formatLastSaved(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);

  if (diffSec < 5) return 'just now';
  if (diffSec < 60) return `${diffSec}s ago`;
  if (diffSec < 3600) return `${Math.floor(diffSec / 60)}m ago`;
  return `${Math.floor(diffSec / 3600)}h ago`;
}

export function AutoSaveIndicator({
  status,
  lastSavedAt,
  isDirty,
  className,
}: AutoSaveIndicatorProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-2 text-sm',
        status === 'error' ? 'text-red-600' : 'text-slate-500',
        className
      )}
    >
      {status === 'saving' && (
        <>
          <Loader2 className="h-3.5 w-3.5 animate-spin" />
          <span>Saving...</span>
        </>
      )}
      {status === 'saved' && (
        <>
          <Check className="h-3.5 w-3.5 text-green-600" />
          <span className="text-green-600">Saved</span>
        </>
      )}
      {status === 'error' && (
        <>
          <AlertCircle className="h-3.5 w-3.5" />
          <span>Save failed</span>
        </>
      )}
      {status === 'idle' && (
        <>
          {isDirty ? (
            <>
              <CloudOff className="h-3.5 w-3.5" />
              <span>Unsaved changes</span>
            </>
          ) : lastSavedAt ? (
            <>
              <Cloud className="h-3.5 w-3.5" />
              <span>Saved {formatLastSaved(lastSavedAt)}</span>
            </>
          ) : (
            <>
              <Cloud className="h-3.5 w-3.5" />
              <span>Auto-save enabled</span>
            </>
          )}
        </>
      )}
    </div>
  );
}
