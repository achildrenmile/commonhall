'use client';

import { useQuery } from '@tanstack/react-query';
import Image from 'next/image';
import Link from 'next/link';
import { Loader2, Newspaper } from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { formatRelativeTime } from '@/lib/utils';
import type { WidgetProps, NewsFeedData } from '../types';

interface NewsArticle {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  featuredImageUrl?: string;
  publishedAt: string;
  author: {
    firstName: string;
    lastName: string;
  };
  channel: {
    slug: string;
    name: string;
  };
}

interface NewsResponse {
  items: NewsArticle[];
  totalCount: number;
}

async function fetchNews(params: NewsFeedData): Promise<NewsArticle[]> {
  const queryParams = new URLSearchParams();
  if (params.channelSlug) queryParams.set('channelSlug', params.channelSlug);
  if (params.spaceSlug) queryParams.set('spaceSlug', params.spaceSlug);
  queryParams.set('limit', String(params.limit || 5));
  queryParams.set('status', 'Published');

  const response = await apiClient.get<NewsResponse>(`/news/articles?${queryParams.toString()}`);
  return response.items;
}

export default function NewsFeedWidget({ data, id }: WidgetProps<NewsFeedData>) {
  const showImages = data.showImages !== false;

  const { data: articles, isLoading, error } = useQuery({
    queryKey: ['widget-news', id, data.channelSlug, data.spaceSlug, data.limit],
    queryFn: () => fetchNews(data),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (error || !articles || articles.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-6 text-center">
        <Newspaper className="h-8 w-8 text-slate-300 dark:text-slate-600 mx-auto mb-2" />
        <p className="text-sm text-slate-500">No news articles available</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {articles.map((article) => (
        <Link
          key={article.id}
          href={`/news/${article.channel.slug}/${article.slug}`}
          className="block group"
        >
          <article className="rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden hover:border-slate-300 dark:hover:border-slate-700 transition-colors">
            <div className={showImages && article.featuredImageUrl ? 'flex' : ''}>
              {showImages && article.featuredImageUrl && (
                <div className="relative w-32 h-24 sm:w-40 sm:h-28 shrink-0">
                  <Image
                    src={article.featuredImageUrl}
                    alt={article.title}
                    fill
                    className="object-cover"
                    sizes="160px"
                  />
                </div>
              )}
              <div className="p-4 flex-1 min-w-0">
                <h3 className="font-medium text-slate-900 dark:text-slate-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors line-clamp-2">
                  {article.title}
                </h3>
                {article.excerpt && (
                  <p className="text-sm text-slate-600 dark:text-slate-400 mt-1 line-clamp-2">
                    {article.excerpt}
                  </p>
                )}
                <div className="flex items-center gap-2 mt-2 text-xs text-slate-500">
                  <span>{article.author.firstName} {article.author.lastName}</span>
                  <span>·</span>
                  <span>{formatRelativeTime(article.publishedAt)}</span>
                </div>
              </div>
            </div>
          </article>
        </Link>
      ))}
    </div>
  );
}
