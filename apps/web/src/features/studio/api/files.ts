import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';

export interface StudioFile {
  id: string;
  name: string;
  originalName: string;
  mimeType: string;
  size: number;
  url: string;
  thumbnailUrl?: string;
  width?: number;
  height?: number;
  createdAt: string;
  uploadedBy: {
    id: string;
    displayName: string;
    avatarUrl?: string;
  };
}

interface FilesListResponse {
  items: StudioFile[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
}

export interface FilesListFilters {
  type?: 'image' | 'document' | 'video' | 'audio' | 'all';
  search?: string;
  collectionId?: string;
}

// List files with infinite scroll
export function useFilesList(filters: FilesListFilters = {}) {
  return useInfiniteQuery({
    queryKey: ['studio-files', filters],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (filters.type && filters.type !== 'all') params.set('type', filters.type);
      if (filters.search) params.set('search', filters.search);
      if (filters.collectionId) params.set('collectionId', filters.collectionId);
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '24');

      return apiClient.get<FilesListResponse>(`/files?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
  });
}

// Get single file
export function useFile(id: string) {
  return useQuery({
    queryKey: ['studio-file', id],
    queryFn: async () => {
      return apiClient.get<StudioFile>(`/files/${id}`);
    },
    enabled: !!id,
  });
}

// Upload file
export function useUploadFile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);

      // Use fetch directly for FormData
      const response = await fetch('/api/files/upload', {
        method: 'POST',
        body: formData,
        credentials: 'include',
      });

      if (!response.ok) {
        throw new Error('Upload failed');
      }

      return response.json() as Promise<StudioFile>;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-files'] });
    },
  });
}

// Delete file
export function useDeleteFile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/files/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['studio-files'] });
    },
  });
}

// File collections
export interface FileCollection {
  id: string;
  name: string;
  fileCount: number;
}

export function useFileCollections() {
  return useQuery({
    queryKey: ['file-collections'],
    queryFn: async () => {
      return apiClient.get<FileCollection[]>('/files/collections');
    },
    staleTime: 5 * 60 * 1000,
  });
}
