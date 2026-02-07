export interface DailyCount {
  date: string;
  count: number;
}

export interface ContentRanking {
  id: string;
  title: string;
  slug?: string;
  views: number;
  uniqueViewers: number;
}

export interface SearchQueryRanking {
  query: string;
  count: number;
  resultCount: number;
}

export interface ChannelDistribution {
  channel: string;
  count: number;
}

export interface DeviceDistribution {
  deviceType: string;
  count: number;
}

export interface ContentAnalytics {
  views: number;
  uniqueViewers: number;
  reactions: number;
  comments: number;
  viewsByDay: DailyCount[];
}

export interface OverviewAnalytics {
  dailyActiveUsers: number;
  monthlyActiveUsers: number;
  pageViews: number;
  articleViews: number;
  topArticles: ContentRanking[];
  topPages: ContentRanking[];
  topSearches: SearchQueryRanking[];
  zeroResultSearches: SearchQueryRanking[];
  channelDistribution: ChannelDistribution[];
  deviceDistribution: DeviceDistribution[];
  dailyActiveUsersByDay: DailyCount[];
  pageViewsByDay: DailyCount[];
}

export interface SearchAnalytics {
  totalSearches: number;
  topQueries: SearchQueryRanking[];
  zeroResultQueries: SearchQueryRanking[];
  clickThroughRate: number;
}

export type DateRangePreset = '7d' | '30d' | '90d' | 'custom';

export interface DateRange {
  from: Date;
  to: Date;
}
