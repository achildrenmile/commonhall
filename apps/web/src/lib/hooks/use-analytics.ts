'use client';

import { useEffect, useRef } from 'react';
import { usePathname } from 'next/navigation';
import { trackPageView } from '../analytics';

/**
 * Hook to automatically track page views on route changes
 */
export function usePageViewTracking(): void {
  const pathname = usePathname();
  const previousPathRef = useRef<string | null>(null);

  useEffect(() => {
    // Only track if path actually changed
    if (previousPathRef.current !== pathname) {
      previousPathRef.current = pathname;
      trackPageView(pathname);
    }
  }, [pathname]);
}

/**
 * Hook to track when an element becomes visible in the viewport
 */
export function useVisibilityTracking(
  callback: () => void,
  options?: IntersectionObserverInit
): React.RefCallback<HTMLElement> {
  const hasTrackedRef = useRef(false);

  return (element: HTMLElement | null) => {
    if (!element || hasTrackedRef.current) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && !hasTrackedRef.current) {
          hasTrackedRef.current = true;
          callback();
          observer.disconnect();
        }
      },
      { threshold: 0.5, ...options }
    );

    observer.observe(element);
  };
}
