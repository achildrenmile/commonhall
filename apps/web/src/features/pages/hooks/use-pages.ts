import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { WidgetBlock } from '../widgets';

export interface Page {
  id: string;
  title: string;
  slug: string;
  content: WidgetBlock[];
  icon?: string;
  description?: string;
  isPublished: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  space?: {
    id: string;
    name: string;
    slug: string;
  };
  parent?: {
    id: string;
    title: string;
    slug: string;
  };
  author: {
    id: string;
    firstName: string;
    lastName: string;
  };
}

export interface PageListItem {
  id: string;
  title: string;
  slug: string;
  icon?: string;
  description?: string;
  isPublished: boolean;
  updatedAt: string;
}

interface PagesResponse {
  items: PageListItem[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
}

interface PageQueryParams {
  spaceSlug?: string;
  parentId?: string;
  limit?: number;
  cursor?: string;
}

// Fetch a single page by space and page slug
export function usePage(spaceSlug: string, pageSlug: string) {
  return useQuery({
    queryKey: ['page', spaceSlug, pageSlug],
    queryFn: async () => {
      return apiClient.get<Page>(`/spaces/${spaceSlug}/pages/${pageSlug}`);
    },
    enabled: !!spaceSlug && !!pageSlug,
  });
}

// Fetch pages list with optional filters
export function usePages(params: PageQueryParams = {}) {
  const queryParams = new URLSearchParams();
  if (params.spaceSlug) queryParams.set('spaceSlug', params.spaceSlug);
  if (params.parentId) queryParams.set('parentId', params.parentId);
  if (params.limit) queryParams.set('limit', String(params.limit));
  if (params.cursor) queryParams.set('cursor', params.cursor);

  return useQuery({
    queryKey: ['pages', params],
    queryFn: async () => {
      return apiClient.get<PagesResponse>(`/pages?${queryParams.toString()}`);
    },
  });
}

// Fetch pages for a specific space
export function useSpacePages(spaceSlug: string) {
  return useQuery({
    queryKey: ['space-pages', spaceSlug],
    queryFn: async () => {
      return apiClient.get<PagesResponse>(`/spaces/${spaceSlug}/pages`);
    },
    enabled: !!spaceSlug,
  });
}

interface CreatePageInput {
  title: string;
  slug: string;
  content: WidgetBlock[];
  spaceId: string;
  parentId?: string;
  icon?: string;
  description?: string;
}

interface UpdatePageInput {
  title?: string;
  slug?: string;
  content?: WidgetBlock[];
  icon?: string;
  description?: string;
  isPublished?: boolean;
}

// Create a new page
export function useCreatePage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreatePageInput) => {
      return apiClient.post<Page>('/pages', input);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['pages'] });
      if (variables.spaceId) {
        queryClient.invalidateQueries({ queryKey: ['space-pages'] });
      }
    },
  });
}

// Update an existing page
export function useUpdatePage(pageId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: UpdatePageInput) => {
      return apiClient.put<Page>(`/pages/${pageId}`, input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pages'] });
      queryClient.invalidateQueries({ queryKey: ['page'] });
      queryClient.invalidateQueries({ queryKey: ['space-pages'] });
    },
  });
}

// Delete a page
export function useDeletePage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (pageId: string) => {
      return apiClient.delete(`/pages/${pageId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pages'] });
      queryClient.invalidateQueries({ queryKey: ['space-pages'] });
    },
  });
}

// Publish/unpublish a page
export function usePublishPage(pageId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (publish: boolean) => {
      return apiClient.post<Page>(`/pages/${pageId}/${publish ? 'publish' : 'unpublish'}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pages'] });
      queryClient.invalidateQueries({ queryKey: ['page'] });
    },
  });
}
