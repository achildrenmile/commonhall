'use client';

import { Suspense } from 'react';
import { HelpCircle } from 'lucide-react';
import { getWidgetComponent, isValidWidgetType } from './registry';
import { WidgetErrorBoundary } from './widget-error-boundary';
import type { WidgetBlock, VisibilityCondition } from './types';
import { useAuthStore } from '@/lib/auth-store';
import { Skeleton } from '@/components/ui/skeleton';

interface WidgetRendererProps {
  widgets: WidgetBlock[];
}

function WidgetSkeleton() {
  return (
    <div className="space-y-3">
      <Skeleton className="h-6 w-3/4" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-5/6" />
    </div>
  );
}

function UnknownWidget({ type }: { type: string }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 dark:border-slate-800 dark:bg-slate-900 p-4">
      <div className="flex items-center gap-3">
        <HelpCircle className="h-5 w-5 text-slate-400" />
        <div>
          <p className="text-sm font-medium text-slate-600 dark:text-slate-400">
            Unknown widget type
          </p>
          <p className="text-xs text-slate-500 dark:text-slate-500">
            Widget type &quot;{type}&quot; is not recognized
          </p>
        </div>
      </div>
    </div>
  );
}

function useVisibilityCheck() {
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (visibility?: VisibilityCondition): boolean => {
    if (!visibility) return true;

    switch (visibility.type) {
      case 'authenticated':
        return isAuthenticated;
      case 'role':
        if (!user || !visibility.value) return false;
        const roleHierarchy = { User: 0, Editor: 1, Admin: 2 };
        const userLevel = roleHierarchy[user.role] ?? 0;
        const requiredLevel = roleHierarchy[visibility.value as keyof typeof roleHierarchy] ?? 0;
        return userLevel >= requiredLevel;
      case 'group':
        // Group visibility would require fetching user groups
        // For now, return true (can be implemented with user group membership)
        return true;
      default:
        return true;
    }
  };
}

export function WidgetRenderer({ widgets }: WidgetRendererProps) {
  const checkVisibility = useVisibilityCheck();

  if (!widgets || widgets.length === 0) {
    return null;
  }

  return (
    <div className="space-y-6">
      {widgets.map((widget) => {
        // Check visibility
        if (!checkVisibility(widget.visibility)) {
          return null;
        }

        // Check if widget type is valid
        if (!isValidWidgetType(widget.type)) {
          return (
            <div key={widget.id}>
              <UnknownWidget type={widget.type} />
            </div>
          );
        }

        const WidgetComponent = getWidgetComponent(widget.type);

        if (!WidgetComponent) {
          return (
            <div key={widget.id}>
              <UnknownWidget type={widget.type} />
            </div>
          );
        }

        return (
          <WidgetErrorBoundary
            key={widget.id}
            widgetId={widget.id}
            widgetType={widget.type}
          >
            <Suspense fallback={<WidgetSkeleton />}>
              <WidgetComponent data={widget.data} id={widget.id} />
            </Suspense>
          </WidgetErrorBoundary>
        );
      })}
    </div>
  );
}
