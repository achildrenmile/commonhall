export interface SearchHit {
  id: string;
  type: 'news' | 'pages' | 'users' | 'files';
  title: string;
  excerpt?: string;
  highlightedTitle?: string;
  highlightedExcerpt?: string;
  url: string;
  imageUrl?: string;
  subtitle?: string;
  date?: string;
  score: number;
  metadata?: Record<string, unknown>;
}

export interface SearchResult {
  hits: SearchHit[];
  total: number;
  typeFacets: Record<string, number>;
  spaceFacets: Record<string, number>;
}

export interface SearchSuggestion {
  id: string;
  type: 'news' | 'pages' | 'users' | 'files';
  title: string;
  subtitle?: string;
  imageUrl?: string;
  url: string;
}

export interface SearchFilters {
  query: string;
  type?: string;
  space?: string;
  from?: number;
  size?: number;
}
