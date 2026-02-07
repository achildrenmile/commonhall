import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type {
  HeadlineRequest,
  HeadlineResponse,
  TeaserRequest,
  TeaserResponse,
  SummarizeRequest,
  SummarizeResponse,
  TranslateRequest,
  TranslateResponse,
  BriefingRequest,
  AskRequest,
  SourceReference,
  ContentHealthReport,
  ContentHealthReportSummary,
} from '../types';

// AI Companion API

export function useGenerateHeadlines() {
  return useMutation({
    mutationFn: async (request: HeadlineRequest) => {
      const response = await api.post<{ data: HeadlineResponse }>('/ai/companion/headline', request);
      return response.data.data;
    },
  });
}

export function useGenerateTeaser() {
  return useMutation({
    mutationFn: async (request: TeaserRequest) => {
      const response = await api.post<{ data: TeaserResponse }>('/ai/companion/teaser', request);
      return response.data.data;
    },
  });
}

export function useSummarize() {
  return useMutation({
    mutationFn: async (request: SummarizeRequest) => {
      const response = await api.post<{ data: SummarizeResponse }>('/ai/companion/summarize', request);
      return response.data.data;
    },
  });
}

export function useTranslate() {
  return useMutation({
    mutationFn: async (request: TranslateRequest) => {
      const response = await api.post<{ data: TranslateResponse }>('/ai/companion/translate', request);
      return response.data.data;
    },
  });
}

// Streaming API helpers

export async function* streamImproveText(
  request: { text: string; instruction: string },
  signal?: AbortSignal
): AsyncGenerator<string, void, undefined> {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/v1/ai/companion/improve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error('Failed to improve text');
  }

  const reader = response.body?.getReader();
  if (!reader) throw new Error('No response body');

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.slice(6);
        if (data === '[DONE]') return;
        yield data.replace(/\\n/g, '\n');
      }
    }
  }
}

export async function* streamDraftFromBriefing(
  request: BriefingRequest,
  signal?: AbortSignal
): AsyncGenerator<string, void, undefined> {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/v1/ai/companion/draft-from-briefing`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error('Failed to draft content');
  }

  const reader = response.body?.getReader();
  if (!reader) throw new Error('No response body');

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.slice(6);
        if (data === '[DONE]') return;
        yield data.replace(/\\n/g, '\n');
      }
    }
  }
}

// AI Search (RAG) API

export interface AskAIResult {
  sources: SourceReference[];
  onChunk: (callback: (chunk: string) => void) => void;
  onDone: (callback: () => void) => void;
  onError: (callback: (error: Error) => void) => void;
}

export async function askAI(
  request: AskRequest,
  signal?: AbortSignal
): Promise<AskAIResult> {
  const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/v1/ai/ask`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
    },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error('Failed to get AI answer');
  }

  const reader = response.body?.getReader();
  if (!reader) throw new Error('No response body');

  let sources: SourceReference[] = [];
  const chunkCallbacks: ((chunk: string) => void)[] = [];
  const doneCallbacks: (() => void)[] = [];
  const errorCallbacks: ((error: Error) => void)[] = [];

  // Start reading in background
  (async () => {
    try {
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('event: sources')) {
            continue;
          }
          if (line.startsWith('event: error')) {
            continue;
          }
          if (line.startsWith('data: ')) {
            const data = line.slice(6);
            if (data === '[DONE]') {
              doneCallbacks.forEach(cb => cb());
              return;
            }
            // Check if this is the sources JSON
            if (data.startsWith('[') || data.startsWith('{')) {
              try {
                sources = JSON.parse(data.replace(/\\n/g, '\n'));
              } catch {
                // Not JSON, treat as chunk
                chunkCallbacks.forEach(cb => cb(data.replace(/\\n/g, '\n')));
              }
            } else {
              chunkCallbacks.forEach(cb => cb(data.replace(/\\n/g, '\n')));
            }
          }
        }
      }
      doneCallbacks.forEach(cb => cb());
    } catch (error) {
      errorCallbacks.forEach(cb => cb(error as Error));
    }
  })();

  return {
    sources,
    onChunk: (callback) => chunkCallbacks.push(callback),
    onDone: (callback) => doneCallbacks.push(callback),
    onError: (callback) => errorCallbacks.push(callback),
  };
}

// Content Health API

export function useContentHealthLatest() {
  return useQuery({
    queryKey: ['content-health', 'latest'],
    queryFn: async () => {
      const response = await api.get<{ data: ContentHealthReport | null }>('/admin/content-health/reports/latest');
      return response.data.data;
    },
  });
}

export function useContentHealthReport(reportId: string | undefined) {
  return useQuery({
    queryKey: ['content-health', reportId],
    queryFn: async () => {
      const response = await api.get<{ data: ContentHealthReport }>(`/admin/content-health/reports/${reportId}`);
      return response.data.data;
    },
    enabled: !!reportId,
  });
}

export function useContentHealthHistory(limit = 10) {
  return useQuery({
    queryKey: ['content-health', 'history', limit],
    queryFn: async () => {
      const response = await api.get<{ data: ContentHealthReportSummary[] }>(`/admin/content-health/reports?limit=${limit}`);
      return response.data.data;
    },
  });
}

export function useStartContentHealthScan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      const response = await api.post<{ data: { reportId: string } }>('/admin/content-health/scan');
      return response.data.data.reportId;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['content-health'] });
    },
  });
}

export function useResolveContentHealthIssue() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (issueId: string) => {
      await api.post(`/admin/content-health/issues/${issueId}/resolve`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['content-health'] });
    },
  });
}
