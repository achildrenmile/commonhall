import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  SurveyListItem,
  SurveyDetail,
  SurveyAnalytics,
  SurveyQuestionInput,
  CreateSurveyInput,
  UpdateSurveyInput,
  SurveyStatus,
} from '../types';

export function useSurveys(status?: SurveyStatus) {
  return useQuery({
    queryKey: ['surveys', status],
    queryFn: async () => {
      const params = status ? `?status=${status}` : '';
      return apiClient.get<SurveyListItem[]>(`/surveys${params}`);
    },
    staleTime: 30 * 1000,
  });
}

export function useSurvey(id: string) {
  return useQuery({
    queryKey: ['survey', id],
    queryFn: async () => {
      return apiClient.get<SurveyDetail>(`/surveys/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

export function useCreateSurvey() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateSurveyInput) => {
      return apiClient.post<SurveyDetail>('/surveys', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['surveys'] });
    },
  });
}

export function useUpdateSurvey() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdateSurveyInput & { id: string }) => {
      return apiClient.put<{ message: string }>(`/surveys/${id}`, input);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['surveys'] });
      queryClient.invalidateQueries({ queryKey: ['survey', variables.id] });
    },
  });
}

export function useUpdateSurveyQuestions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, questions }: { id: string; questions: SurveyQuestionInput[] }) => {
      return apiClient.put<{ message: string }>(`/surveys/${id}/questions`, { questions });
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['survey', variables.id] });
    },
  });
}

export function useActivateSurvey() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<{ message: string }>(`/surveys/${id}/activate`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['surveys'] });
      queryClient.invalidateQueries({ queryKey: ['survey', id] });
    },
  });
}

export function useCloseSurvey() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<{ message: string }>(`/surveys/${id}/close`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['surveys'] });
      queryClient.invalidateQueries({ queryKey: ['survey', id] });
    },
  });
}

export function useSurveyAnalytics(id: string) {
  return useQuery({
    queryKey: ['survey-analytics', id],
    queryFn: async () => {
      return apiClient.get<SurveyAnalytics>(`/surveys/${id}/analytics`);
    },
    enabled: !!id,
    refetchInterval: 60000,
  });
}

export function useDeleteSurvey() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/surveys/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['surveys'] });
    },
  });
}

export function exportSurveyCsv(id: string): string {
  return `/api/v1/surveys/${id}/export`;
}
