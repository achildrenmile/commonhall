import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { SearchResult, SearchSuggestion, SearchFilters } from '../types';

export function useSearch(filters: SearchFilters) {
  return useQuery({
    queryKey: ['search', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('q', filters.query);
      if (filters.type) params.set('type', filters.type);
      if (filters.space) params.set('space', filters.space);
      if (filters.from !== undefined) params.set('from', String(filters.from));
      if (filters.size !== undefined) params.set('size', String(filters.size));

      return apiClient.get<SearchResult>(`/search?${params.toString()}`);
    },
    enabled: filters.query.length >= 2,
    staleTime: 60 * 1000, // 1 minute
  });
}

export function useSearchSuggestions(query: string) {
  return useQuery({
    queryKey: ['search-suggestions', query],
    queryFn: async () => {
      const params = new URLSearchParams({ q: query, limit: '6' });
      return apiClient.get<SearchSuggestion[]>(`/search/suggest?${params.toString()}`);
    },
    enabled: query.length >= 2,
    staleTime: 30 * 1000,
  });
}
