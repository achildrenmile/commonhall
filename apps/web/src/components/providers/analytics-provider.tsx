'use client';

import { usePageViewTracking } from '@/lib/hooks/use-analytics';

interface AnalyticsProviderProps {
  children: React.ReactNode;
}

export function AnalyticsProvider({ children }: AnalyticsProviderProps) {
  // Auto-track page views on route changes
  usePageViewTracking();

  return <>{children}</>;
}
