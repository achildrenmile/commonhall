import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';

export interface StudioStats {
  publishedArticlesThisMonth: number;
  pageViewsThisWeek: number;
  pendingComments: number;
  activeUsersToday: number;
}

export function useStudioStats() {
  return useQuery({
    queryKey: ['studio-stats'],
    queryFn: async () => {
      return apiClient.get<StudioStats>('/studio/stats');
    },
    staleTime: 60 * 1000, // 1 minute
  });
}

// Recent activity types
export interface RecentActivity {
  id: string;
  type: 'article_published' | 'page_created' | 'comment_added' | 'file_uploaded';
  title: string;
  description: string;
  timestamp: string;
  userId: string;
  userName: string;
  userAvatarUrl?: string;
}

export function useRecentActivity() {
  return useQuery({
    queryKey: ['studio-recent-activity'],
    queryFn: async () => {
      return apiClient.get<RecentActivity[]>('/studio/activity?limit=10');
    },
    staleTime: 60 * 1000,
  });
}
