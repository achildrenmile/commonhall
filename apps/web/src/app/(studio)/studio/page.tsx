'use client';

import Link from 'next/link';
import { Newspaper, FileText, Eye, Folder, MessageSquare, Users, Plus, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { useStudioStats } from '@/features/studio/api';

function StatCard({
  title,
  value,
  description,
  icon: Icon,
  trend,
}: {
  title: string;
  value: number | string;
  description: string;
  icon: React.ElementType;
  trend?: { value: number; label: string };
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-slate-600 dark:text-slate-400">
          {title}
        </CardTitle>
        <Icon className="h-4 w-4 text-slate-400" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          {value}
        </div>
        <p className="text-xs text-slate-500 mt-1">{description}</p>
        {trend && (
          <div className="flex items-center gap-1 mt-2 text-xs">
            <TrendingUp className="h-3 w-3 text-green-500" />
            <span className="text-green-600 dark:text-green-400 font-medium">
              +{trend.value}%
            </span>
            <span className="text-slate-500">{trend.label}</span>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function StatCardSkeleton() {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-4 w-4" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-8 w-16 mb-1" />
        <Skeleton className="h-3 w-32" />
      </CardContent>
    </Card>
  );
}

const quickActions = [
  {
    title: 'New Article',
    description: 'Write and publish news',
    icon: Newspaper,
    href: '/studio/news/new',
    color: 'bg-blue-500/10 text-blue-600 dark:text-blue-400',
  },
  {
    title: 'New Page',
    description: 'Create a knowledge page',
    icon: FileText,
    href: '/studio/pages/new',
    color: 'bg-purple-500/10 text-purple-600 dark:text-purple-400',
  },
  {
    title: 'View Site',
    description: 'Preview your content',
    icon: Eye,
    href: '/',
    color: 'bg-green-500/10 text-green-600 dark:text-green-400',
    external: true,
  },
];

export default function StudioDashboardPage() {
  const { data: stats, isLoading } = useStudioStats();

  return (
    <div className="max-w-6xl mx-auto space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
          Dashboard
        </h1>
        <p className="text-slate-600 dark:text-slate-400 mt-1">
          Welcome back! Here's what's happening with your content.
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {isLoading ? (
          <>
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
          </>
        ) : (
          <>
            <StatCard
              title="Published Articles"
              value={stats?.publishedArticlesThisMonth ?? 0}
              description="This month"
              icon={Newspaper}
            />
            <StatCard
              title="Page Views"
              value={stats?.pageViewsThisWeek?.toLocaleString() ?? '0'}
              description="This week"
              icon={Eye}
            />
            <StatCard
              title="Pending Comments"
              value={stats?.pendingComments ?? 0}
              description="Awaiting review"
              icon={MessageSquare}
            />
            <StatCard
              title="Active Users"
              value={stats?.activeUsersToday ?? 0}
              description="Today"
              icon={Users}
            />
          </>
        )}
      </div>

      {/* Quick Actions */}
      <div>
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">
          Quick Actions
        </h2>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {quickActions.map((action) => (
            <Link
              key={action.href}
              href={action.href}
              target={action.external ? '_blank' : undefined}
            >
              <Card className="hover:shadow-md transition-shadow cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-4">
                    <div className={`p-3 rounded-lg ${action.color}`}>
                      <action.icon className="h-5 w-5" />
                    </div>
                    <div className="flex-1">
                      <CardTitle className="text-base flex items-center gap-2">
                        {action.title}
                        <Plus className="h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" />
                      </CardTitle>
                      <CardDescription>{action.description}</CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>
          ))}
        </div>
      </div>

      {/* Recent Content & Activity */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Recent Articles */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle className="text-base">Recent Articles</CardTitle>
              <CardDescription>Your latest published content</CardDescription>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/studio/news">View all</Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="text-sm text-slate-500 text-center py-8">
              No recent articles
            </div>
          </CardContent>
        </Card>

        {/* Recent Pages */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle className="text-base">Recent Pages</CardTitle>
              <CardDescription>Recently updated pages</CardDescription>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link href="/studio/pages">View all</Link>
            </Button>
          </CardHeader>
          <CardContent>
            <div className="text-sm text-slate-500 text-center py-8">
              No recent pages
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
