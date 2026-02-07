import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { WidgetBlock } from '@/features/pages/widgets';

export interface Space {
  id: string;
  name: string;
  slug: string;
  description?: string;
  coverImageUrl?: string;
  iconUrl?: string;
  isPublic: boolean;
  homepageContent?: WidgetBlock[];
  createdAt: string;
  updatedAt: string;
  parent?: {
    id: string;
    name: string;
    slug: string;
  };
}

export interface SpaceListItem {
  id: string;
  name: string;
  slug: string;
  description?: string;
  iconUrl?: string;
  isPublic: boolean;
}

export interface SpaceWithChildren extends Space {
  pages: Array<{
    id: string;
    title: string;
    slug: string;
    icon?: string;
    description?: string;
  }>;
  childSpaces: Array<{
    id: string;
    name: string;
    slug: string;
    description?: string;
    iconUrl?: string;
  }>;
}

interface SpacesResponse {
  items: SpaceListItem[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
}

interface SpaceQueryParams {
  parentId?: string;
  limit?: number;
  cursor?: string;
}

// Fetch a single space by slug with pages and child spaces
export function useSpace(slug: string) {
  return useQuery({
    queryKey: ['space', slug],
    queryFn: async () => {
      return apiClient.get<SpaceWithChildren>(`/spaces/${slug}`);
    },
    enabled: !!slug,
  });
}

// Fetch spaces list with optional filters
export function useSpaces(params: SpaceQueryParams = {}) {
  const queryParams = new URLSearchParams();
  if (params.parentId) queryParams.set('parentId', params.parentId);
  if (params.limit) queryParams.set('limit', String(params.limit));
  if (params.cursor) queryParams.set('cursor', params.cursor);

  return useQuery({
    queryKey: ['spaces', params],
    queryFn: async () => {
      return apiClient.get<SpacesResponse>(`/spaces?${queryParams.toString()}`);
    },
  });
}

// Fetch root spaces (spaces without a parent)
export function useRootSpaces() {
  return useQuery({
    queryKey: ['spaces', 'root'],
    queryFn: async () => {
      return apiClient.get<SpacesResponse>('/spaces?rootOnly=true');
    },
  });
}

interface CreateSpaceInput {
  name: string;
  slug: string;
  description?: string;
  parentId?: string;
  isPublic?: boolean;
}

interface UpdateSpaceInput {
  name?: string;
  slug?: string;
  description?: string;
  coverImageUrl?: string;
  iconUrl?: string;
  isPublic?: boolean;
  homepageContent?: WidgetBlock[];
}

// Create a new space
export function useCreateSpace() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateSpaceInput) => {
      return apiClient.post<Space>('/spaces', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces'] });
    },
  });
}

// Update an existing space
export function useUpdateSpace(spaceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: UpdateSpaceInput) => {
      return apiClient.put<Space>(`/spaces/${spaceId}`, input);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['spaces'] });
      queryClient.invalidateQueries({ queryKey: ['space', data.slug] });
    },
  });
}

// Delete a space
export function useDeleteSpace() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (spaceId: string) => {
      return apiClient.delete(`/spaces/${spaceId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['spaces'] });
    },
  });
}
