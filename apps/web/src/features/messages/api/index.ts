import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  ConversationListItem,
  ConversationDetail,
  Message,
  CreateConversationInput,
  UnreadCountResponse,
} from '../types';

interface PaginatedResponse<T> {
  items: T[];
  nextCursor?: string;
  hasMore: boolean;
}

export function useConversations() {
  return useQuery({
    queryKey: ['conversations'],
    queryFn: async () => {
      return apiClient.get<ConversationListItem[]>('/chat/conversations');
    },
    staleTime: 30 * 1000,
  });
}

export function useConversation(conversationId: string) {
  return useQuery({
    queryKey: ['conversation', conversationId],
    queryFn: async () => {
      return apiClient.get<ConversationDetail>(`/chat/conversations/${conversationId}`);
    },
    enabled: !!conversationId,
  });
}

export function useCreateConversation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateConversationInput) => {
      return apiClient.post<{ id: string }>('/chat/conversations', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useMessages(conversationId: string) {
  return useInfiniteQuery({
    queryKey: ['messages', conversationId],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '50');
      return apiClient.get<PaginatedResponse<Message>>(
        `/chat/conversations/${conversationId}/messages?${params.toString()}`
      );
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.nextCursor : undefined),
    enabled: !!conversationId,
  });
}

export function useSendMessage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ conversationId, body }: { conversationId: string; body: string }) => {
      return apiClient.post<{ id: string }>(`/chat/conversations/${conversationId}/messages`, {
        body,
      });
    },
    onSuccess: (_, { conversationId }) => {
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useEditMessage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      conversationId,
      messageId,
      body,
    }: {
      conversationId: string;
      messageId: string;
      body: string;
    }) => {
      return apiClient.put(`/chat/conversations/${conversationId}/messages/${messageId}`, { body });
    },
    onSuccess: (_, { conversationId }) => {
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] });
    },
  });
}

export function useDeleteMessage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      conversationId,
      messageId,
    }: {
      conversationId: string;
      messageId: string;
    }) => {
      return apiClient.delete(`/chat/conversations/${conversationId}/messages/${messageId}`);
    },
    onSuccess: (_, { conversationId }) => {
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] });
    },
  });
}

export function useMarkAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (conversationId: string) => {
      return apiClient.put(`/chat/conversations/${conversationId}/read`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['unread-count'] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
    },
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: ['unread-count'],
    queryFn: async () => {
      return apiClient.get<UnreadCountResponse>('/chat/unread-count');
    },
    staleTime: 10 * 1000,
    refetchInterval: 30 * 1000,
  });
}
