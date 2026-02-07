import { useQuery, useInfiniteQuery } from '@tanstack/react-query';
import { apiClient } from '@/lib/api-client';

// Types
export interface FacetItem {
  name: string;
  count: number;
}

export interface SearchFacets {
  departments: FacetItem[];
  locations: FacetItem[];
}

export interface Person {
  id: string;
  email: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  avatarUrl?: string;
  department?: string;
  location?: string;
  jobTitle?: string;
  phoneNumber?: string;
  bio?: string;
  role: 'Employee' | 'Editor' | 'Admin';
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface PersonGroup {
  id: string;
  name: string;
  slug: string;
  description?: string;
}

export interface PersonArticle {
  id: string;
  title: string;
  slug: string;
  teaserImageUrl?: string;
  publishedAt?: string;
  channelName: string;
  channelSlug: string;
}

export interface PersonProfile extends Person {
  groups: PersonGroup[];
  recentArticles: PersonArticle[];
}

export interface SearchUsersResult {
  items: Person[];
  totalCount: number;
  hasNextPage: boolean;
  nextCursor?: string;
  facets: SearchFacets;
}

export interface PeopleSearchFilters {
  query?: string;
  departments?: string[];
  locations?: string[];
}

// Search People Hook with Infinite Scroll
export function usePeopleSearch(filters: PeopleSearchFilters = {}) {
  return useInfiniteQuery({
    queryKey: ['people-search', filters],
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (filters.query) params.set('q', filters.query);
      if (filters.departments?.length) params.set('department', filters.departments.join(','));
      if (filters.locations?.length) params.set('location', filters.locations.join(','));
      if (pageParam) params.set('cursor', pageParam);
      params.set('size', '24');

      return apiClient.get<SearchUsersResult>(`/people/search?${params.toString()}`);
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.hasNextPage ? lastPage.nextCursor : undefined,
    staleTime: 60 * 1000, // 1 minute
  });
}

// User Profile Hook
export function useUserProfile(userId: string) {
  return useQuery({
    queryKey: ['user-profile', userId],
    queryFn: async () => {
      return apiClient.get<PersonProfile>(`/people/${userId}/profile`);
    },
    enabled: !!userId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

// Departments Hook
export function useDepartments() {
  return useQuery({
    queryKey: ['departments'],
    queryFn: async () => {
      return apiClient.get<FacetItem[]>('/people/departments');
    },
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

// Locations Hook
export function useLocations() {
  return useQuery({
    queryKey: ['locations'],
    queryFn: async () => {
      return apiClient.get<FacetItem[]>('/people/locations');
    },
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}

// Get facets from first page of search (useful for filter sidebar)
export function usePeopleFacets() {
  return useQuery({
    queryKey: ['people-facets'],
    queryFn: async () => {
      const result = await apiClient.get<SearchUsersResult>('/people/search?size=0');
      return result.facets;
    },
    staleTime: 5 * 60 * 1000,
  });
}
