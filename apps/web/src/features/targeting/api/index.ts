import { useQuery, useMutation } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';
import type { TargetingPreview, TargetingSchema, VisibilityRule } from '../types';

// Get targeting schema
export function useTargetingSchema() {
  return useQuery({
    queryKey: ['targeting-schema'],
    queryFn: async () => {
      return apiClient.get<TargetingSchema>('/targeting/schema');
    },
    staleTime: 60 * 60 * 1000, // 1 hour - schema rarely changes
  });
}

// Get preview for a visibility rule
export function useTargetingPreview(ruleJson: string | null, enabled = true) {
  return useQuery({
    queryKey: ['targeting-preview', ruleJson],
    queryFn: async () => {
      const params = ruleJson ? `?ruleJson=${encodeURIComponent(ruleJson)}` : '';
      return apiClient.get<TargetingPreview>(`/targeting/preview${params}`);
    },
    enabled: enabled,
    staleTime: 30 * 1000, // 30 seconds
  });
}

// Evaluate a rule for debugging
export function useEvaluateRule() {
  return useMutation({
    mutationFn: async (params: { userId?: string; ruleJson?: string }) => {
      return apiClient.post<{ userId: string; isVisible: boolean; ruleJson?: string }>(
        '/targeting/evaluate',
        params
      );
    },
  });
}

// Helper to convert a VisibilityRule to JSON string
export function ruleToJson(rule: VisibilityRule | null): string | null {
  if (!rule || rule.type === 'all') return null;
  return JSON.stringify(rule);
}

// Helper to parse JSON to VisibilityRule
export function jsonToRule(json: string | null | undefined): VisibilityRule {
  if (!json) return { type: 'all' };
  try {
    return JSON.parse(json) as VisibilityRule;
  } catch {
    return { type: 'all' };
  }
}
