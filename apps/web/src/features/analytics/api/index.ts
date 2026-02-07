import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { OverviewAnalytics, ContentAnalytics, SearchAnalytics } from '../types';

export function useOverviewAnalytics(from: Date, to: Date) {
  return useQuery({
    queryKey: ['analytics', 'overview', from.toISOString(), to.toISOString()],
    queryFn: async () => {
      const params = new URLSearchParams({
        from: from.toISOString(),
        to: to.toISOString(),
      });
      return apiClient.get<OverviewAnalytics>(`/analytics/overview?${params.toString()}`);
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useContentAnalytics(targetType: string, targetId: string, from: Date, to: Date) {
  return useQuery({
    queryKey: ['analytics', 'content', targetType, targetId, from.toISOString(), to.toISOString()],
    queryFn: async () => {
      const params = new URLSearchParams({
        from: from.toISOString(),
        to: to.toISOString(),
      });
      return apiClient.get<ContentAnalytics>(
        `/analytics/content/${targetType}/${targetId}?${params.toString()}`
      );
    },
    enabled: !!targetType && !!targetId,
    staleTime: 5 * 60 * 1000,
  });
}

export function useSearchAnalytics(from: Date, to: Date) {
  return useQuery({
    queryKey: ['analytics', 'search', from.toISOString(), to.toISOString()],
    queryFn: async () => {
      const params = new URLSearchParams({
        from: from.toISOString(),
        to: to.toISOString(),
      });
      return apiClient.get<SearchAnalytics>(`/analytics/search?${params.toString()}`);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function getExportUrl(type: 'overview' | 'search', from: Date, to: Date): string {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  const params = new URLSearchParams({
    type,
    format: 'csv',
    from: from.toISOString(),
    to: to.toISOString(),
  });
  return `${apiUrl}/api/v1/analytics/export?${params.toString()}`;
}
