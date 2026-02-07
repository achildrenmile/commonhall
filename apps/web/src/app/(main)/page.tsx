'use client';

import Link from 'next/link';
import { formatDistanceToNow } from '@/lib/date-utils';
import { useAuthStore } from '@/lib/auth-store';
import { useNewsFeed, type NewsArticle } from '@/features/news/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Newspaper, FolderOpen, Users, FileText, ArrowRight } from 'lucide-react';

function NewsCard({ article }: { article: NewsArticle }) {
  const author = article.displayAuthor || article.author;

  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardHeader className="pb-3">
        <div className="flex items-start gap-3">
          {article.teaserImageUrl && (
            <div className="relative h-16 w-24 shrink-0 overflow-hidden rounded-md bg-slate-100 dark:bg-slate-800">
              <img
                src={article.teaserImageUrl}
                alt=""
                className="h-full w-full object-cover"
              />
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              {article.channel && (
                <Badge variant="secondary" className="text-xs">
                  {article.channel.name}
                </Badge>
              )}
              {article.isPinned && (
                <Badge variant="outline" className="text-xs">Pinned</Badge>
              )}
            </div>
            <CardTitle className="text-base line-clamp-2">
              <Link href={`/news/${article.slug}`} className="hover:underline">
                {article.title}
              </Link>
            </CardTitle>
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        {article.teaserText && (
          <p className="text-sm text-muted-foreground line-clamp-2 mb-3">
            {article.teaserText}
          </p>
        )}
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <div className="flex items-center gap-2">
            <Avatar className="h-5 w-5">
              <AvatarImage src={author.avatarUrl} />
              <AvatarFallback className="text-[10px]">
                {author.displayName.charAt(0)}
              </AvatarFallback>
            </Avatar>
            <span>{author.displayName}</span>
          </div>
          <span>
            {article.publishedAt && formatDistanceToNow(article.publishedAt)}
          </span>
        </div>
      </CardContent>
    </Card>
  );
}

function NewsCardSkeleton() {
  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-start gap-3">
          <Skeleton className="h-16 w-24 rounded-md" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-20" />
            <Skeleton className="h-5 w-full" />
            <Skeleton className="h-5 w-3/4" />
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <Skeleton className="h-4 w-full mb-2" />
        <Skeleton className="h-4 w-2/3" />
      </CardContent>
    </Card>
  );
}

interface QuickLinkProps {
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  description: string;
}

function QuickLink({ href, icon: Icon, title, description }: QuickLinkProps) {
  return (
    <Link href={href}>
      <Card className="h-full hover:shadow-md transition-shadow cursor-pointer group">
        <CardHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 text-primary">
              <Icon className="h-5 w-5" />
            </div>
            <div className="flex-1">
              <CardTitle className="text-base flex items-center gap-2">
                {title}
                <ArrowRight className="h-4 w-4 opacity-0 -translate-x-2 group-hover:opacity-100 group-hover:translate-x-0 transition-all" />
              </CardTitle>
              <CardDescription className="text-sm">{description}</CardDescription>
            </div>
          </div>
        </CardHeader>
      </Card>
    </Link>
  );
}

export default function DashboardPage() {
  const user = useAuthStore((state) => state.user);
  const { data: newsData, isLoading: newsLoading } = useNewsFeed({ size: 5 });

  const greeting = getGreeting();

  return (
    <div className="space-y-8">
      {/* Welcome section */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          {greeting}, {user?.firstName || 'there'}!
        </h1>
        <p className="text-muted-foreground mt-1">
          Here&apos;s what&apos;s happening in your organization today.
        </p>
      </div>

      {/* Quick links */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <QuickLink
          href="/news"
          icon={Newspaper}
          title="News"
          description="Latest updates and announcements"
        />
        <QuickLink
          href="/spaces"
          icon={FolderOpen}
          title="Spaces"
          description="Browse team workspaces"
        />
        <QuickLink
          href="/people"
          icon={Users}
          title="People"
          description="Find colleagues and teams"
        />
        <QuickLink
          href="/studio"
          icon={FileText}
          title="Create"
          description="Write a new article or page"
        />
      </div>

      {/* Recent news */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">
            Recent News
          </h2>
          <Link
            href="/news"
            className="text-sm text-primary hover:underline flex items-center gap-1"
          >
            View all <ArrowRight className="h-4 w-4" />
          </Link>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
          {newsLoading ? (
            <>
              <NewsCardSkeleton />
              <NewsCardSkeleton />
              <NewsCardSkeleton />
              <NewsCardSkeleton />
              <NewsCardSkeleton />
            </>
          ) : newsData?.items && newsData.items.length > 0 ? (
            newsData.items.map((article) => (
              <NewsCard key={article.id} article={article} />
            ))
          ) : (
            <Card className="col-span-full">
              <CardContent className="flex flex-col items-center justify-center py-12">
                <Newspaper className="h-12 w-12 text-muted-foreground mb-4" />
                <p className="text-muted-foreground">No news articles yet</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
}
