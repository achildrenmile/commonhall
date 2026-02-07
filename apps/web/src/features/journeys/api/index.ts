import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type {
  Journey,
  JourneyDetail,
  JourneyEnrollment,
  JourneyAnalytics,
  JourneyStepInput,
  CreateJourneyInput,
  UpdateJourneyInput,
  JourneyTriggerType,
  JourneyEnrollmentStatus,
  MyJourney,
  MyJourneyDetail,
} from '../types';

// Admin API

export function useJourneys(activeOnly?: boolean, triggerType?: JourneyTriggerType) {
  return useQuery({
    queryKey: ['journeys', activeOnly, triggerType],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (activeOnly !== undefined) params.set('activeOnly', String(activeOnly));
      if (triggerType) params.set('triggerType', triggerType);
      return apiClient.get<Journey[]>(`/journeys?${params.toString()}`);
    },
    staleTime: 30 * 1000,
  });
}

export function useJourney(id: string) {
  return useQuery({
    queryKey: ['journey', id],
    queryFn: async () => {
      return apiClient.get<JourneyDetail>(`/journeys/${id}`);
    },
    enabled: !!id && id !== 'new',
  });
}

export function useCreateJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (input: CreateJourneyInput) => {
      return apiClient.post<JourneyDetail>('/journeys', input);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['journeys'] });
    },
  });
}

export function useUpdateJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, ...input }: UpdateJourneyInput & { id: string }) => {
      return apiClient.put<{ message: string }>(`/journeys/${id}`, input);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['journeys'] });
      queryClient.invalidateQueries({ queryKey: ['journey', variables.id] });
    },
  });
}

export function useUpdateJourneySteps() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, steps }: { id: string; steps: JourneyStepInput[] }) => {
      return apiClient.put<{ message: string }>(`/journeys/${id}/steps`, { steps });
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['journey', variables.id] });
    },
  });
}

export function useActivateJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<{ message: string }>(`/journeys/${id}/activate`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['journeys'] });
      queryClient.invalidateQueries({ queryKey: ['journey', id] });
    },
  });
}

export function useDeactivateJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      return apiClient.post<{ message: string }>(`/journeys/${id}/deactivate`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['journeys'] });
      queryClient.invalidateQueries({ queryKey: ['journey', id] });
    },
  });
}

export function useEnrollUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ journeyId, userId }: { journeyId: string; userId: string }) => {
      return apiClient.post<{ message: string }>(`/journeys/${journeyId}/enroll`, { userId });
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['journey-enrollments', variables.journeyId] });
      queryClient.invalidateQueries({ queryKey: ['journey', variables.journeyId] });
    },
  });
}

interface EnrollmentListResponse {
  items: JourneyEnrollment[];
  nextCursor?: string;
  hasMore: boolean;
}

export function useJourneyEnrollments(journeyId: string, status?: JourneyEnrollmentStatus) {
  return useInfiniteQuery({
    queryKey: ['journey-enrollments', journeyId, status],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (status) params.set('status', status);
      if (pageParam) params.set('cursor', pageParam);
      params.set('limit', '50');
      return apiClient.get<EnrollmentListResponse>(`/journeys/${journeyId}/enrollments?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasMore ? lastPage.nextCursor : undefined,
    enabled: !!journeyId,
  });
}

export function useJourneyAnalytics(id: string) {
  return useQuery({
    queryKey: ['journey-analytics', id],
    queryFn: async () => {
      return apiClient.get<JourneyAnalytics>(`/journeys/${id}/analytics`);
    },
    enabled: !!id,
    refetchInterval: 60000,
  });
}

export function useDeleteJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/journeys/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['journeys'] });
    },
  });
}

// User-facing API

export function useMyJourneys(status?: JourneyEnrollmentStatus) {
  return useQuery({
    queryKey: ['my-journeys', status],
    queryFn: async () => {
      const params = status ? `?status=${status}` : '';
      return apiClient.get<MyJourney[]>(`/me/journeys${params}`);
    },
    staleTime: 30 * 1000,
  });
}

export function useMyJourneyDetail(enrollmentId: string) {
  return useQuery({
    queryKey: ['my-journey', enrollmentId],
    queryFn: async () => {
      return apiClient.get<MyJourneyDetail>(`/me/journeys/${enrollmentId}`);
    },
    enabled: !!enrollmentId,
  });
}

export function useMarkStepViewed() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ enrollmentId, stepIndex }: { enrollmentId: string; stepIndex: number }) => {
      return apiClient.post<{ message: string }>(`/me/journeys/${enrollmentId}/steps/${stepIndex}/view`);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['my-journey', variables.enrollmentId] });
    },
  });
}

export function useCompleteStep() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ enrollmentId, stepIndex }: { enrollmentId: string; stepIndex: number }) => {
      return apiClient.post<{ message: string }>(`/me/journeys/${enrollmentId}/steps/${stepIndex}/complete`);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['my-journeys'] });
      queryClient.invalidateQueries({ queryKey: ['my-journey', variables.enrollmentId] });
    },
  });
}

export function usePauseMyJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (enrollmentId: string) => {
      return apiClient.post<{ message: string }>(`/me/journeys/${enrollmentId}/pause`);
    },
    onSuccess: (_, enrollmentId) => {
      queryClient.invalidateQueries({ queryKey: ['my-journeys'] });
      queryClient.invalidateQueries({ queryKey: ['my-journey', enrollmentId] });
    },
  });
}

export function useResumeMyJourney() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (enrollmentId: string) => {
      return apiClient.post<{ message: string }>(`/me/journeys/${enrollmentId}/resume`);
    },
    onSuccess: (_, enrollmentId) => {
      queryClient.invalidateQueries({ queryKey: ['my-journeys'] });
      queryClient.invalidateQueries({ queryKey: ['my-journey', enrollmentId] });
    },
  });
}
