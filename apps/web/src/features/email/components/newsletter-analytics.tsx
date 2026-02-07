'use client';

import { useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { format } from 'date-fns';
import {
  ChevronLeft,
  Users,
  Send,
  Eye,
  MousePointerClick,
  AlertCircle,
  Loader2,
  ExternalLink,
  TrendingUp,
} from 'lucide-react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { useNewsletter, useNewsletterAnalytics } from '../api';

interface NewsletterAnalyticsProps {
  id: string;
}

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

function StatCard({
  title,
  value,
  subValue,
  icon: Icon,
  trend,
}: {
  title: string;
  value: string | number;
  subValue?: string;
  icon: React.ElementType;
  trend?: 'up' | 'down' | null;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="flex items-baseline gap-2">
          <span className="text-2xl font-bold">{value}</span>
          {trend && (
            <TrendingUp
              className={`h-4 w-4 ${trend === 'up' ? 'text-green-500' : 'text-red-500 rotate-180'}`}
            />
          )}
        </div>
        {subValue && <p className="text-xs text-muted-foreground mt-1">{subValue}</p>}
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
        <Skeleton className="h-8 w-20 mb-1" />
        <Skeleton className="h-3 w-32" />
      </CardContent>
    </Card>
  );
}

export function NewsletterAnalytics({ id }: NewsletterAnalyticsProps) {
  const router = useRouter();
  const { data: newsletter, isLoading: isLoadingNewsletter } = useNewsletter(id);
  const { data: analytics, isLoading: isLoadingAnalytics } = useNewsletterAnalytics(id);

  const isLoading = isLoadingNewsletter || isLoadingAnalytics;

  const timelineData = useMemo(() => {
    if (!analytics?.openTimeline) return [];
    return analytics.openTimeline.map((point) => ({
      time: format(new Date(point.time), 'MMM d HH:mm'),
      opens: point.value,
    }));
  }, [analytics?.openTimeline]);

  const deviceData = useMemo(() => {
    if (!analytics?.deviceBreakdown) return [];
    return analytics.deviceBreakdown.map((device) => ({
      name: device.device,
      value: device.count,
      percentage: device.percentage,
    }));
  }, [analytics?.deviceBreakdown]);

  if (isLoading) {
    return (
      <div className="space-y-6 p-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div>
            <Skeleton className="h-6 w-48 mb-2" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCardSkeleton />
          <StatCardSkeleton />
          <StatCardSkeleton />
          <StatCardSkeleton />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <Card className="lg:col-span-2">
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-[300px] w-full" />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-[300px] w-full" />
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  if (!newsletter || !analytics) {
    return (
      <div className="flex flex-col items-center justify-center h-96 gap-4">
        <AlertCircle className="h-12 w-12 text-muted-foreground" />
        <p className="text-muted-foreground">Newsletter not found</p>
        <Button variant="outline" onClick={() => router.push('/studio/email')}>
          Back to Newsletters
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => router.push('/studio/email')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold">{newsletter.title}</h1>
          <p className="text-muted-foreground">
            Sent on {format(new Date(newsletter.sentAt || newsletter.createdAt), 'MMMM d, yyyy')}
          </p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
        <StatCard
          title="Total Recipients"
          value={analytics.totalRecipients.toLocaleString()}
          icon={Users}
        />
        <StatCard
          title="Delivered"
          value={analytics.delivered.toLocaleString()}
          subValue={`${((analytics.delivered / analytics.totalRecipients) * 100).toFixed(1)}% delivery rate`}
          icon={Send}
        />
        <StatCard
          title="Opened"
          value={analytics.opened.toLocaleString()}
          subValue={`${analytics.openRate.toFixed(1)}% open rate`}
          icon={Eye}
          trend={analytics.openRate >= 25 ? 'up' : analytics.openRate >= 15 ? null : 'down'}
        />
        <StatCard
          title="Clicked"
          value={analytics.clicked.toLocaleString()}
          subValue={`${analytics.clickRate.toFixed(1)}% click rate`}
          icon={MousePointerClick}
          trend={analytics.clickRate >= 3 ? 'up' : analytics.clickRate >= 1 ? null : 'down'}
        />
        <StatCard
          title="Click-to-Open"
          value={`${analytics.clickToOpenRate.toFixed(1)}%`}
          subValue="Of those who opened"
          icon={TrendingUp}
        />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Opens Over Time */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Opens Over Time</CardTitle>
            <CardDescription>Cumulative email opens since sending</CardDescription>
          </CardHeader>
          <CardContent>
            {timelineData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={timelineData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="time" tick={{ fontSize: 12 }} tickLine={false} />
                  <YAxis tick={{ fontSize: 12 }} tickLine={false} axisLine={false} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: 'hsl(var(--popover))',
                      border: '1px solid hsl(var(--border))',
                      borderRadius: '8px',
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="opens"
                    stroke="#3b82f6"
                    strokeWidth={2}
                    dot={false}
                    activeDot={{ r: 4 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[300px] flex items-center justify-center text-muted-foreground">
                No data available yet
              </div>
            )}
          </CardContent>
        </Card>

        {/* Device Breakdown */}
        <Card>
          <CardHeader>
            <CardTitle>Device Breakdown</CardTitle>
            <CardDescription>Email opens by device type</CardDescription>
          </CardHeader>
          <CardContent>
            {deviceData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={deviceData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={90}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {deviceData.map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(value: number, name: string) => [
                      `${value} (${deviceData.find((d) => d.name === name)?.percentage.toFixed(1)}%)`,
                      name,
                    ]}
                    contentStyle={{
                      backgroundColor: 'hsl(var(--popover))',
                      border: '1px solid hsl(var(--border))',
                      borderRadius: '8px',
                    }}
                  />
                  <Legend
                    formatter={(value) => <span className="text-sm">{value}</span>}
                    layout="vertical"
                    align="right"
                    verticalAlign="middle"
                  />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[300px] flex items-center justify-center text-muted-foreground">
                No data available yet
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Top Links Table */}
      <Card>
        <CardHeader>
          <CardTitle>Top Clicked Links</CardTitle>
          <CardDescription>Links with the most clicks in this newsletter</CardDescription>
        </CardHeader>
        <CardContent>
          {analytics.topLinks.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>URL</TableHead>
                  <TableHead className="text-right">Total Clicks</TableHead>
                  <TableHead className="text-right">Unique Clicks</TableHead>
                  <TableHead className="text-right">Click Rate</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {analytics.topLinks.map((link, index) => (
                  <TableRow key={index}>
                    <TableCell>
                      <a
                        href={link.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="flex items-center gap-1 text-primary hover:underline max-w-md truncate"
                      >
                        {link.url}
                        <ExternalLink className="h-3 w-3 flex-shrink-0" />
                      </a>
                    </TableCell>
                    <TableCell className="text-right">{link.clicks.toLocaleString()}</TableCell>
                    <TableCell className="text-right">
                      {link.uniqueClicks.toLocaleString()}
                    </TableCell>
                    <TableCell className="text-right">
                      {analytics.totalRecipients > 0
                        ? ((link.uniqueClicks / analytics.totalRecipients) * 100).toFixed(1)
                        : 0}
                      %
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <div className="py-12 text-center text-muted-foreground">
              No link clicks recorded yet
            </div>
          )}
        </CardContent>
      </Card>

      {/* Bounced Info */}
      {analytics.bounced > 0 && (
        <Card className="border-destructive/50">
          <CardHeader>
            <CardTitle className="text-destructive flex items-center gap-2">
              <AlertCircle className="h-5 w-5" />
              Bounced Emails
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground">
              {analytics.bounced.toLocaleString()} emails bounced (
              {((analytics.bounced / analytics.totalRecipients) * 100).toFixed(1)}% bounce rate).
              Consider cleaning your email list to improve deliverability.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
