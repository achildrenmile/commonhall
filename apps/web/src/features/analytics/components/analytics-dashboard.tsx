'use client';

import { useState, useMemo } from 'react';
import { subDays } from 'date-fns';
import { Download, Users, Eye, FileText, Search, Loader2, Printer } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useOverviewAnalytics, useSearchAnalytics, getExportUrl } from '../api';
import { DateRangePicker } from './date-range-picker';
import { StatCard } from './stat-card';
import { DailyChart, TopContentChart, SearchQueriesChart, DeviceChart } from './analytics-charts';
import type { DateRange, SearchQueryRanking } from '../types';

export function AnalyticsDashboard() {
  const [dateRange, setDateRange] = useState<DateRange>({
    from: subDays(new Date(), 30),
    to: new Date(),
  });

  const { data: overview, isLoading: overviewLoading } = useOverviewAnalytics(
    dateRange.from,
    dateRange.to
  );

  const { data: searchData, isLoading: searchLoading } = useSearchAnalytics(
    dateRange.from,
    dateRange.to
  );

  const handlePrint = () => {
    window.print();
  };

  const handleExport = (type: 'overview' | 'search') => {
    const url = getExportUrl(type, dateRange.from, dateRange.to);
    window.open(url, '_blank');
  };

  const isLoading = overviewLoading || searchLoading;

  return (
    <div className="space-y-6 print:space-y-4">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 print:hidden">
        <div>
          <h1 className="text-2xl font-bold">Analytics</h1>
          <p className="text-sm text-muted-foreground">
            Track engagement and content performance
          </p>
        </div>
        <div className="flex items-center gap-2">
          <DateRangePicker value={dateRange} onChange={setDateRange} />
          <Button variant="outline" size="icon" onClick={handlePrint}>
            <Printer className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Print header */}
      <div className="hidden print:block">
        <h1 className="text-2xl font-bold">Analytics Report</h1>
        <p className="text-sm text-muted-foreground">
          {dateRange.from.toLocaleDateString()} - {dateRange.to.toLocaleDateString()}
        </p>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : (
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList className="print:hidden">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="content">Content</TabsTrigger>
            <TabsTrigger value="search">Search</TabsTrigger>
          </TabsList>

          {/* Overview Tab */}
          <TabsContent value="overview" className="space-y-6">
            {/* Stat Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <StatCard
                title="Daily Active Users"
                value={overview?.dailyActiveUsers ?? 0}
                icon={<Users className="h-5 w-5" />}
              />
              <StatCard
                title="Monthly Active Users"
                value={overview?.monthlyActiveUsers ?? 0}
                icon={<Users className="h-5 w-5" />}
              />
              <StatCard
                title="Page Views"
                value={overview?.pageViews ?? 0}
                icon={<Eye className="h-5 w-5" />}
              />
              <StatCard
                title="Article Views"
                value={overview?.articleViews ?? 0}
                icon={<FileText className="h-5 w-5" />}
              />
            </div>

            {/* DAU + Views Chart */}
            <div className="bg-card border rounded-lg p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-medium">Daily Active Users & Page Views</h3>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleExport('overview')}
                  className="print:hidden"
                >
                  <Download className="h-4 w-4 mr-2" />
                  Export CSV
                </Button>
              </div>
              {overview?.dailyActiveUsersByDay && overview?.pageViewsByDay && (
                <DailyChart
                  data={overview.dailyActiveUsersByDay}
                  secondaryData={overview.pageViewsByDay}
                  primaryLabel="Active Users"
                  secondaryLabel="Page Views"
                />
              )}
            </div>

            {/* Device Distribution */}
            <div className="grid lg:grid-cols-2 gap-6">
              <div className="bg-card border rounded-lg p-6">
                {overview?.deviceDistribution && (
                  <DeviceChart data={overview.deviceDistribution} />
                )}
              </div>
              <div className="bg-card border rounded-lg p-6">
                <h3 className="font-medium mb-4">Channel Distribution</h3>
                <div className="space-y-3">
                  {overview?.channelDistribution.map((channel) => {
                    const total = overview.channelDistribution.reduce((s, c) => s + c.count, 0);
                    const percent = total > 0 ? (channel.count / total) * 100 : 0;
                    return (
                      <div key={channel.channel}>
                        <div className="flex justify-between text-sm mb-1">
                          <span className="capitalize">{channel.channel}</span>
                          <span className="text-muted-foreground">
                            {channel.count.toLocaleString()} ({percent.toFixed(1)}%)
                          </span>
                        </div>
                        <div className="h-2 bg-muted rounded-full overflow-hidden">
                          <div
                            className="h-full bg-primary rounded-full"
                            style={{ width: `${percent}%` }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          </TabsContent>

          {/* Content Tab */}
          <TabsContent value="content" className="space-y-6">
            <div className="grid lg:grid-cols-2 gap-6">
              <div className="bg-card border rounded-lg p-6">
                {overview?.topArticles && (
                  <TopContentChart data={overview.topArticles} title="Top Articles" />
                )}
              </div>
              <div className="bg-card border rounded-lg p-6">
                {overview?.topPages && (
                  <TopContentChart data={overview.topPages} title="Top Pages" />
                )}
              </div>
            </div>

            {/* Top Content Table */}
            <div className="bg-card border rounded-lg p-6">
              <h3 className="font-medium mb-4">Top Performing Content</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-4">Title</th>
                      <th className="text-right py-3 px-4">Views</th>
                      <th className="text-right py-3 px-4">Unique Viewers</th>
                      <th className="text-right py-3 px-4">Type</th>
                    </tr>
                  </thead>
                  <tbody>
                    {[
                      ...(overview?.topArticles.map((a) => ({ ...a, type: 'Article' })) || []),
                      ...(overview?.topPages.map((p) => ({ ...p, type: 'Page' })) || []),
                    ]
                      .sort((a, b) => b.views - a.views)
                      .slice(0, 15)
                      .map((item) => (
                        <tr key={item.id} className="border-b last:border-0 hover:bg-muted/50">
                          <td className="py-3 px-4 font-medium">{item.title}</td>
                          <td className="py-3 px-4 text-right">{item.views.toLocaleString()}</td>
                          <td className="py-3 px-4 text-right">{item.uniqueViewers.toLocaleString()}</td>
                          <td className="py-3 px-4 text-right text-muted-foreground">{item.type}</td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
            </div>
          </TabsContent>

          {/* Search Tab */}
          <TabsContent value="search" className="space-y-6">
            {/* Search Stats */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <StatCard
                title="Total Searches"
                value={searchData?.totalSearches ?? 0}
                icon={<Search className="h-5 w-5" />}
              />
              <StatCard
                title="Click-Through Rate"
                value={`${searchData?.clickThroughRate ?? 0}%`}
                icon={<Eye className="h-5 w-5" />}
              />
              <StatCard
                title="Top Queries"
                value={searchData?.topQueries.length ?? 0}
              />
              <StatCard
                title="Zero Result Queries"
                value={searchData?.zeroResultQueries.length ?? 0}
              />
            </div>

            <div className="grid lg:grid-cols-2 gap-6">
              <div className="bg-card border rounded-lg p-6">
                {searchData?.topQueries && (
                  <SearchQueriesChart data={searchData.topQueries} title="Top Search Queries" />
                )}
              </div>
              <div className="bg-card border rounded-lg p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="font-medium">Zero Result Searches</h3>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleExport('search')}
                    className="print:hidden"
                  >
                    <Download className="h-4 w-4 mr-2" />
                    Export CSV
                  </Button>
                </div>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="text-left py-2 px-3">Query</th>
                        <th className="text-right py-2 px-3">Count</th>
                      </tr>
                    </thead>
                    <tbody>
                      {searchData?.zeroResultQueries.slice(0, 10).map((query) => (
                        <tr key={query.query} className="border-b last:border-0">
                          <td className="py-2 px-3">{query.query}</td>
                          <td className="py-2 px-3 text-right">{query.count}</td>
                        </tr>
                      ))}
                      {(!searchData?.zeroResultQueries || searchData.zeroResultQueries.length === 0) && (
                        <tr>
                          <td colSpan={2} className="py-4 text-center text-muted-foreground">
                            No zero-result searches in this period
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </TabsContent>
        </Tabs>
      )}
    </div>
  );
}
