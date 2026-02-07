import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  CommunityListItem,
  CommunityDetail,
  CommunityMember,
  CommunityPost,
  CommunityComment,
  CreateCommunityInput,
} from '../types';

interface PaginatedResponse<T> {
  items: T[];
  nextCursor?: string;
  hasMore: boolean;
}

export function useCommunities(search?: string, myOnly?: boolean) {
  return useQuery({
    queryKey: ['communities', search, myOnly],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (search) params.set('search', search);
      if (myOnly) params.set('myOnly', 'true');
      return apiClient.get<CommunityListItem[]>(`/communities?${params.toString()}`);
    },
    staleTime: 30 * 1000,
  });
}

export function useCommunity(slug: string) {
  return useQuery({
    queryKey: ['community', slug],
    queryFn: async () => {
      return apiClient.get<CommunityDetail>(`/communities/${slug}`);
    },
    enabled: !!slug,
  });
}

export function useCreateCommunity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateCommunityInput) => {
      return apiClient.post<{ id: string; slug: string }>('/communities', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['communities'] });
    },
  });
}

export function useJoinCommunity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (slug: string) => {
      return apiClient.post<{ message: string }>(`/communities/${slug}/join`);
    },
    onSuccess: (_, slug) => {
      queryClient.invalidateQueries({ queryKey: ['communities'] });
      queryClient.invalidateQueries({ queryKey: ['community', slug] });
    },
  });
}

export function useLeaveCommunity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (slug: string) => {
      return apiClient.post<{ message: string }>(`/communities/${slug}/leave`);
    },
    onSuccess: (_, slug) => {
      queryClient.invalidateQueries({ queryKey: ['communities'] });
      queryClient.invalidateQueries({ queryKey: ['community', slug] });
    },
  });
}

export function useCommunityMembers(slug: string) {
  return useInfiniteQuery({
    queryKey: ['community-members', slug],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '50');
      return apiClient.get<PaginatedResponse<CommunityMember>>(`/communities/${slug}/members?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
    enabled: !!slug,
  });
}

export function useCommunityPosts(slug: string) {
  return useInfiniteQuery({
    queryKey: ['community-posts', slug],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '20');
      return apiClient.get<PaginatedResponse<CommunityPost>>(`/communities/${slug}/posts?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
    enabled: !!slug,
  });
}

export function useCreatePost() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ slug, body, imageUrl }: { slug: string; body: string; imageUrl?: string }) => {
      return apiClient.post<{ id: string }>(`/communities/${slug}/posts`, { body, imageUrl });
    },
    onSuccess: (_, { slug }) => {
      queryClient.invalidateQueries({ queryKey: ['community-posts', slug] });
    },
  });
}

export function useLikePost() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ slug, postId }: { slug: string; postId: string }) => {
      return apiClient.post<{ liked: boolean; likeCount: number }>(`/communities/${slug}/posts/${postId}/like`);
    },
    onSuccess: (_, { slug }) => {
      queryClient.invalidateQueries({ queryKey: ['community-posts', slug] });
    },
  });
}

export function usePostComments(slug: string, postId: string) {
  return useQuery({
    queryKey: ['post-comments', slug, postId],
    queryFn: async () => {
      return apiClient.get<CommunityComment[]>(`/communities/${slug}/posts/${postId}/comments`);
    },
    enabled: !!slug && !!postId,
  });
}

export function useAddComment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ slug, postId, body }: { slug: string; postId: string; body: string }) => {
      return apiClient.post<{ id: string }>(`/communities/${slug}/posts/${postId}/comments`, { body });
    },
    onSuccess: (_, { slug, postId }) => {
      queryClient.invalidateQueries({ queryKey: ['post-comments', slug, postId] });
      queryClient.invalidateQueries({ queryKey: ['community-posts', slug] });
    },
  });
}
