import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { WidgetBlock } from '@/features/pages/widgets';

export interface StudioPage {
  id: string;
  title: string;
  slug: string;
  icon?: string;
  description?: string;
  content: WidgetBlock[];
  isPublished: boolean;
  publishedAt?: string;
  spaceId: string;
  space: {
    id: string;
    name: string;
    slug: string;
  };
  parentId?: string;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface PageTreeNode {
  id: string;
  title: string;
  slug: string;
  icon?: string;
  isPublished: boolean;
  spaceId: string;
  parentId?: string;
  order: number;
  children: PageTreeNode[];
}

export interface SpaceWithPages {
  id: string;
  name: string;
  slug: string;
  iconUrl?: string;
  pages: PageTreeNode[];
}

// Get pages tree by space
export function usePageTree() {
  return useQuery({
    queryKey: ['page-tree'],
    queryFn: async () => {
      return apiClient.get<SpaceWithPages[]>('/studio/pages/tree');
    },
    staleTime: 30 * 1000,
  });
}

// Get single page
export function useStudioPage(id: string) {
  return useQuery({
    queryKey: ['studio-page', id],
    queryFn: async () => {
      return apiClient.get<StudioPage>(`/studio/pages/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

// Create page
export interface CreatePageInput {
  title: string;
  slug?: string;
  icon?: string;
  description?: string;
  content?: WidgetBlock[];
  spaceId: string;
  parentId?: string;
}

export function useCreatePage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreatePageInput) => {
      return apiClient.post<StudioPage>('/studio/pages', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
    },
  });
}

// Update page
export interface UpdatePageInput {
  id: string;
  title?: string;
  slug?: string;
  icon?: string;
  description?: string;
  content?: WidgetBlock[];
  isPublished?: boolean;
}

export function useUpdatePage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdatePageInput) => {
      return apiClient.put<StudioPage>(`/studio/pages/${id}`, input);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
      queryClient.setQueryData(['studio-page', data.id], data);
    },
  });
}

// Delete page
export function useDeletePage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/studio/pages/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
    },
  });
}

// Publish page
export function usePublishPage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioPage>(`/studio/pages/${id}/publish`);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
      queryClient.setQueryData(['studio-page', data.id], data);
    },
  });
}

// Unpublish page
export function useUnpublishPage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<StudioPage>(`/studio/pages/${id}/unpublish`);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
      queryClient.setQueryData(['studio-page', data.id], data);
    },
  });
}

// Reorder pages
export interface ReorderPagesInput {
  pageId: string;
  parentId?: string;
  newIndex: number;
}

export function useReorderPages() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: ReorderPagesInput) => {
      await apiClient.post('/studio/pages/reorder', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['page-tree'] });
    },
  });
}
