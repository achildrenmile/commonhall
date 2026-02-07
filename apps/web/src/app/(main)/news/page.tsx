'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useNewsFeed, type NewsArticle } from '@/features/news/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { Search, Newspaper, MessageSquare, Heart, Eye } from 'lucide-react';
import { formatDistanceToNow } from '@/lib/date-utils';

function NewsListCard({ article }: { article: NewsArticle }) {
  const author = article.displayAuthor || article.author;

  return (
    <Card className="hover:shadow-md transition-shadow">
      <div className="flex">
        {article.teaserImageUrl && (
          <div className="relative hidden sm:block h-full w-48 shrink-0 overflow-hidden rounded-l-lg bg-slate-100 dark:bg-slate-800">
            <img
              src={article.teaserImageUrl}
              alt=""
              className="h-full w-full object-cover"
            />
          </div>
        )}
        <div className="flex-1">
          <CardHeader className="pb-2">
            <div className="flex items-center gap-2 mb-1">
              {article.channel && (
                <Badge
                  variant="secondary"
                  className="text-xs"
                  style={article.channel.color ? { backgroundColor: article.channel.color + '20', color: article.channel.color } : undefined}
                >
                  {article.channel.name}
                </Badge>
              )}
              {article.isPinned && (
                <Badge variant="outline" className="text-xs">Pinned</Badge>
              )}
              {article.tags?.slice(0, 2).map((tag) => (
                <Badge key={tag.id} variant="outline" className="text-xs">
                  {tag.name}
                </Badge>
              ))}
            </div>
            <CardTitle className="text-lg line-clamp-2">
              <Link href={`/news/${article.slug}`} className="hover:underline">
                {article.title}
              </Link>
            </CardTitle>
          </CardHeader>
          <CardContent className="pt-0">
            {article.teaserText && (
              <p className="text-sm text-muted-foreground line-clamp-2 mb-3">
                {article.teaserText}
              </p>
            )}
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Avatar className="h-6 w-6">
                  <AvatarImage src={author.avatarUrl} />
                  <AvatarFallback className="text-xs">
                    {author.displayName.charAt(0)}
                  </AvatarFallback>
                </Avatar>
                <span>{author.displayName}</span>
                <span>·</span>
                <span>{article.publishedAt && formatDistanceToNow(article.publishedAt)}</span>
              </div>
              <div className="flex items-center gap-4">
                <span className="flex items-center gap-1">
                  <Eye className="h-4 w-4" /> {article.viewCount}
                </span>
                <span className="flex items-center gap-1">
                  <Heart className="h-4 w-4" /> {article.likeCount}
                </span>
                <span className="flex items-center gap-1">
                  <MessageSquare className="h-4 w-4" /> {article.commentCount}
                </span>
              </div>
            </div>
          </CardContent>
        </div>
      </div>
    </Card>
  );
}

function NewsListSkeleton() {
  return (
    <Card>
      <div className="flex">
        <Skeleton className="hidden sm:block h-32 w-48 rounded-l-lg" />
        <div className="flex-1 p-6 space-y-3">
          <div className="flex gap-2">
            <Skeleton className="h-5 w-20" />
            <Skeleton className="h-5 w-16" />
          </div>
          <Skeleton className="h-6 w-3/4" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </div>
    </Card>
  );
}

export default function NewsPage() {
  const [search, setSearch] = useState('');
  const { data, isLoading } = useNewsFeed({ search: search || undefined, size: 20 });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">News</h1>
          <p className="text-muted-foreground">
            Stay updated with the latest announcements and stories
          </p>
        </div>
        <div className="relative w-full sm:w-72">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search news..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* News list */}
      <div className="space-y-4">
        {isLoading ? (
          <>
            <NewsListSkeleton />
            <NewsListSkeleton />
            <NewsListSkeleton />
          </>
        ) : data?.items && data.items.length > 0 ? (
          data.items.map((article) => (
            <NewsListCard key={article.id} article={article} />
          ))
        ) : (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-16">
              <Newspaper className="h-16 w-16 text-muted-foreground mb-4" />
              <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
                No news articles found
              </h3>
              <p className="text-muted-foreground mt-1">
                {search ? 'Try adjusting your search terms' : 'Check back later for updates'}
              </p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Load more */}
      {data?.meta?.hasMore && (
        <div className="flex justify-center">
          <Button variant="outline">Load more</Button>
        </div>
      )}
    </div>
  );
}
