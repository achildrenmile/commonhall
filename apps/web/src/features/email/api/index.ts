import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  EmailTemplate,
  EmailTemplateDetail,
  Newsletter,
  NewsletterDetail,
  NewsletterAnalytics,
  CreateNewsletterInput,
  UpdateNewsletterInput,
  NewsletterStatus,
} from '../types';

// Templates

export function useEmailTemplates(category?: string) {
  return useQuery({
    queryKey: ['email-templates', category],
    queryFn: async () => {
      const params = category ? `?category=${category}` : '';
      return apiClient.get<EmailTemplate[]>(`/email/templates${params}`);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function useEmailTemplate(id: string) {
  return useQuery({
    queryKey: ['email-template', id],
    queryFn: async () => {
      return apiClient.get<EmailTemplateDetail>(`/email/templates/${id}`);
    },
    enabled: !!id,
  });
}

export function useCreateEmailTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: { name: string; description?: string; content?: string; category?: string }) => {
      return apiClient.post<EmailTemplateDetail>('/email/templates', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates'] });
    },
  });
}

export function useUpdateEmailTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: { id: string; name?: string; description?: string; content?: string }) => {
      return apiClient.put<EmailTemplateDetail>(`/email/templates/${id}`, input);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['email-templates'] });
      queryClient.setQueryData(['email-template', data.id], data);
    },
  });
}

export function useDeleteEmailTemplate() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/email/templates/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates'] });
    },
  });
}

// Newsletters

interface NewsletterListResponse {
  items: Newsletter[];
  nextCursor?: string;
  hasMore: boolean;
}

export function useNewsletters(status?: NewsletterStatus) {
  return useInfiniteQuery({
    queryKey: ['newsletters', status],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (status) params.set('status', status);
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '20');

      return apiClient.get<NewsletterListResponse>(`/newsletters?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
  });
}

export function useNewsletter(id: string) {
  return useQuery({
    queryKey: ['newsletter', id],
    queryFn: async () => {
      return apiClient.get<NewsletterDetail>(`/newsletters/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

export function useCreateNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateNewsletterInput) => {
      return apiClient.post<NewsletterDetail>('/newsletters', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['newsletters'] });
    },
  });
}

export function useUpdateNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdateNewsletterInput & { id: string }) => {
      return apiClient.put<NewsletterDetail>(`/newsletters/${id}`, input);
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['newsletters'] });
      queryClient.setQueryData(['newsletter', data.id], data);
    },
  });
}

export function useDeleteNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/newsletters/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['newsletters'] });
    },
  });
}

export function useSendNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<{ message: string }>(`/newsletters/${id}/send`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['newsletters'] });
    },
  });
}

export function useScheduleNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, scheduledAt }: { id: string; scheduledAt: string }) => {
      return apiClient.post<{ message: string; scheduledAt: string }>(`/newsletters/${id}/schedule`, { scheduledAt });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['newsletters'] });
    },
  });
}

export function useSendTestNewsletter() {
  return useMutation({
    mutationFn: async ({ id, email }: { id: string; email: string }) => {
      return apiClient.post<{ message: string }>(`/newsletters/${id}/test`, { email });
    },
  });
}

export function useNewsletterAnalytics(id: string) {
  return useQuery({
    queryKey: ['newsletter-analytics', id],
    queryFn: async () => {
      return apiClient.get<NewsletterAnalytics>(`/newsletters/${id}/analytics`);
    },
    enabled: !!id,
    refetchInterval: 30000, // Refresh every 30 seconds for live updates
  });
}

export function useNewsletterPreview(id: string) {
  return useQuery({
    queryKey: ['newsletter-preview', id],
    queryFn: async () => {
      const response = await fetch(`/api/v1/newsletters/${id}/preview`, {
        credentials: 'include',
      });
      return response.text();
    },
    enabled: !!id && id !== 'new',
    staleTime: 0,
  });
}
