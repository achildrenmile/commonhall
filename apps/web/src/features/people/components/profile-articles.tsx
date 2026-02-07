'use client';

import Image from 'next/image';
import Link from 'next/link';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { formatDate } from '@/lib/utils';
import type { PersonArticle } from '../api';

interface ProfileArticlesProps {
  articles: PersonArticle[];
}

export function ProfileArticles({ articles }: ProfileArticlesProps) {
  if (articles.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Recent Articles</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {articles.map((article) => (
            <Link
              key={article.id}
              href={`/news/${article.slug}`}
              className="group flex gap-4 p-3 -mx-3 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-900 transition-colors"
            >
              {/* Thumbnail */}
              {article.teaserImageUrl ? (
                <div className="relative h-16 w-24 shrink-0 rounded-md overflow-hidden bg-slate-100 dark:bg-slate-800">
                  <Image
                    src={article.teaserImageUrl}
                    alt={article.title}
                    fill
                    className="object-cover"
                    sizes="96px"
                  />
                </div>
              ) : (
                <div className="h-16 w-24 shrink-0 rounded-md bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900 flex items-center justify-center">
                  <span className="text-2xl">📰</span>
                </div>
              )}

              {/* Content */}
              <div className="flex-1 min-w-0">
                <Badge variant="outline" className="text-xs mb-1">
                  {article.channelName}
                </Badge>
                <h3 className="font-medium text-slate-900 dark:text-slate-100 line-clamp-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                  {article.title}
                </h3>
                {article.publishedAt && (
                  <p className="text-xs text-slate-500 mt-1">
                    {formatDate(article.publishedAt)}
                  </p>
                )}
              </div>
            </Link>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
