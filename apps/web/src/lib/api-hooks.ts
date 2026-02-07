import {
  useQuery,
  useMutation,
  type UseQueryOptions,
  type UseMutationOptions,
  type QueryKey,
} from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { apiClient } from './api-client';

// Generic API query hook
export function useApiQuery<TData>(
  queryKey: QueryKey,
  url: string,
  options?: Omit<UseQueryOptions<TData, AxiosError>, 'queryKey' | 'queryFn'>
) {
  return useQuery<TData, AxiosError>({
    queryKey,
    queryFn: async () => {
      const response = await apiClient.get<TData>(url);
      return response.data;
    },
    ...options,
  });
}

// Generic API mutation hook
export function useApiMutation<TData, TVariables>(
  mutationFn: (variables: TVariables) => Promise<TData>,
  options?: Omit<UseMutationOptions<TData, AxiosError, TVariables>, 'mutationFn'>
) {
  return useMutation<TData, AxiosError, TVariables>({
    mutationFn,
    ...options,
  });
}

// Paginated query response type
export interface PaginatedResponse<T> {
  items: T[];
  meta?: {
    hasMore?: boolean;
    nextCursor?: string;
    total?: number;
  };
}

// Paginated query hook with cursor support
export function usePaginatedQuery<TItem>(
  queryKey: QueryKey,
  url: string,
  params?: Record<string, unknown>,
  options?: Omit<UseQueryOptions<PaginatedResponse<TItem>, AxiosError>, 'queryKey' | 'queryFn'>
) {
  return useQuery<PaginatedResponse<TItem>, AxiosError>({
    queryKey: [...queryKey, params],
    queryFn: async () => {
      const response = await apiClient.get(url, { params });
      // Handle both direct array response and paginated response
      if (Array.isArray(response.data)) {
        return { items: response.data };
      }
      return {
        items: response.data,
        meta: (response as unknown as { meta?: PaginatedResponse<TItem>['meta'] }).meta,
      };
    },
    ...options,
  });
}

// POST mutation helper
export function usePostMutation<TData, TVariables>(
  url: string,
  options?: Omit<UseMutationOptions<TData, AxiosError, TVariables>, 'mutationFn'>
) {
  return useMutation<TData, AxiosError, TVariables>({
    mutationFn: async (variables) => {
      const response = await apiClient.post<TData>(url, variables);
      return response.data;
    },
    ...options,
  });
}

// PUT mutation helper
export function usePutMutation<TData, TVariables>(
  url: string | ((variables: TVariables) => string),
  options?: Omit<UseMutationOptions<TData, AxiosError, TVariables>, 'mutationFn'>
) {
  return useMutation<TData, AxiosError, TVariables>({
    mutationFn: async (variables) => {
      const endpoint = typeof url === 'function' ? url(variables) : url;
      const response = await apiClient.put<TData>(endpoint, variables);
      return response.data;
    },
    ...options,
  });
}

// DELETE mutation helper
export function useDeleteMutation<TVariables = void>(
  url: string | ((variables: TVariables) => string),
  options?: Omit<UseMutationOptions<void, AxiosError, TVariables>, 'mutationFn'>
) {
  return useMutation<void, AxiosError, TVariables>({
    mutationFn: async (variables) => {
      const endpoint = typeof url === 'function' ? url(variables) : url;
      await apiClient.delete(endpoint);
    },
    ...options,
  });
}
