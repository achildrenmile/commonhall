import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  FormListItem,
  FormDetail,
  FormSubmission,
  SubmissionsResponse,
  CreateFormInput,
  UpdateFormInput,
} from '../types';

export function useForms(isActive?: boolean) {
  return useQuery({
    queryKey: ['forms', isActive],
    queryFn: async () => {
      const params = isActive !== undefined ? `?isActive=${isActive}` : '';
      return apiClient.get<FormListItem[]>(`/forms${params}`);
    },
    staleTime: 30 * 1000,
  });
}

export function useForm(id: string) {
  return useQuery({
    queryKey: ['form', id],
    queryFn: async () => {
      return apiClient.get<FormDetail>(`/forms/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

export function useCreateForm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateFormInput) => {
      return apiClient.post<FormDetail>('/forms', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['forms'] });
    },
  });
}

export function useUpdateForm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdateFormInput & { id: string }) => {
      return apiClient.put<{ message: string }>(`/forms/${id}`, input);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['forms'] });
      queryClient.invalidateQueries({ queryKey: ['form', variables.id] });
    },
  });
}

export function useDeleteForm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/forms/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['forms'] });
    },
  });
}

export function useFormSubmissions(formId: string) {
  return useInfiniteQuery({
    queryKey: ['form-submissions', formId],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '50');
      return apiClient.get<SubmissionsResponse>(`/forms/${formId}/submissions?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
    enabled: !!formId,
  });
}

export function exportFormCsv(id: string): string {
  return `/api/v1/forms/${id}/export`;
}
