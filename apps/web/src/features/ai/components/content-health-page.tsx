'use client';

import { useState } from 'react';
import { format, formatDistanceToNow } from 'date-fns';
import {
  Activity,
  AlertTriangle,
  Link2Off,
  Clock,
  Eye,
  FileX,
  Play,
  Loader2,
  Check,
  ExternalLink,
  RefreshCw,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { cn } from '@/lib/utils';
import {
  useContentHealthLatest,
  useContentHealthHistory,
  useStartContentHealthScan,
  useResolveContentHealthIssue,
} from '../api';
import type { ContentHealthIssue } from '../types';

const issueTypeIcons: Record<string, React.ElementType> = {
  stale: Clock,
  broken_link: Link2Off,
  unused: FileX,
  low_engagement: Eye,
};

const issueTypeLabels: Record<string, string> = {
  stale: 'Stale Content',
  broken_link: 'Broken Link',
  unused: 'Unused File',
  low_engagement: 'Low Engagement',
};

const severityColors: Record<string, string> = {
  low: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300',
  medium: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300',
  high: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
};

export function ContentHealthPage() {
  const [issueTypeFilter, setIssueTypeFilter] = useState<string>('all');
  const [severityFilter, setSeverityFilter] = useState<string>('all');

  const { data: report, isLoading, refetch } = useContentHealthLatest();
  const { data: history } = useContentHealthHistory(5);
  const startScan = useStartContentHealthScan();
  const resolveIssue = useResolveContentHealthIssue();

  const handleStartScan = async () => {
    await startScan.mutateAsync();
    // Start polling for updates
    const interval = setInterval(async () => {
      const result = await refetch();
      if (result.data?.status === 'completed' || result.data?.status === 'failed') {
        clearInterval(interval);
      }
    }, 3000);
  };

  const handleResolve = async (issueId: string) => {
    await resolveIssue.mutateAsync(issueId);
  };

  const filteredIssues = report?.issues.filter(issue => {
    if (issueTypeFilter !== 'all' && issue.issueType !== issueTypeFilter) return false;
    if (severityFilter !== 'all' && issue.severity !== severityFilter) return false;
    if (issue.isResolved) return false;
    return true;
  }) ?? [];

  const unresolvedCount = report?.issues.filter(i => !i.isResolved).length ?? 0;
  const resolvedCount = report?.issues.filter(i => i.isResolved).length ?? 0;
  const healthScore = report && report.issues.length > 0
    ? Math.round((1 - unresolvedCount / (report.totalContentCount || 1)) * 100)
    : 100;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Content Health</h1>
          <p className="text-muted-foreground">
            Monitor and maintain the quality of your content
          </p>
        </div>
        <Button
          onClick={handleStartScan}
          disabled={startScan.isPending || report?.status === 'running'}
        >
          {startScan.isPending || report?.status === 'running' ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Scanning...
            </>
          ) : (
            <>
              <Play className="mr-2 h-4 w-4" />
              Start Scan
            </>
          )}
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Health Score</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{healthScore}%</div>
            <Progress value={healthScore} className="mt-2" />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Stale Content</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{report?.staleContentCount ?? 0}</div>
            <p className="text-xs text-muted-foreground">
              Not updated in 6+ months
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Broken Links</CardTitle>
            <Link2Off className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{report?.brokenLinkCount ?? 0}</div>
            <p className="text-xs text-muted-foreground">
              External links not working
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Low Engagement</CardTitle>
            <Eye className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{report?.lowEngagementCount ?? 0}</div>
            <p className="text-xs text-muted-foreground">
              Few views after 30+ days
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Summary */}
      {report?.summary && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">AI Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">{report.summary}</p>
          </CardContent>
        </Card>
      )}

      {/* Issues Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Issues</CardTitle>
              <CardDescription>
                {unresolvedCount} unresolved issues • {resolvedCount} resolved
              </CardDescription>
            </div>
            <div className="flex gap-2">
              <Select value={issueTypeFilter} onValueChange={setIssueTypeFilter}>
                <SelectTrigger className="w-[160px]">
                  <SelectValue placeholder="Issue type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Types</SelectItem>
                  <SelectItem value="stale">Stale Content</SelectItem>
                  <SelectItem value="broken_link">Broken Links</SelectItem>
                  <SelectItem value="unused">Unused Files</SelectItem>
                  <SelectItem value="low_engagement">Low Engagement</SelectItem>
                </SelectContent>
              </Select>
              <Select value={severityFilter} onValueChange={setSeverityFilter}>
                <SelectTrigger className="w-[140px]">
                  <SelectValue placeholder="Severity" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Severities</SelectItem>
                  <SelectItem value="high">High</SelectItem>
                  <SelectItem value="medium">Medium</SelectItem>
                  <SelectItem value="low">Low</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : filteredIssues.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <AlertTriangle className="h-8 w-8 mx-auto mb-2" />
              <p>No issues found matching your filters</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Issue</TableHead>
                  <TableHead>Content</TableHead>
                  <TableHead>Severity</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredIssues.map((issue) => {
                  const Icon = issueTypeIcons[issue.issueType] || AlertTriangle;
                  return (
                    <TableRow key={issue.id}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Icon className="h-4 w-4 text-muted-foreground" />
                          <span className="font-medium">
                            {issueTypeLabels[issue.issueType] || issue.issueType}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <div>
                          <a
                            href={issue.contentUrl}
                            className="font-medium hover:underline flex items-center gap-1"
                          >
                            {issue.contentTitle}
                            <ExternalLink className="h-3 w-3" />
                          </a>
                          <Badge variant="outline" className="mt-1 text-xs">
                            {issue.contentType}
                          </Badge>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge className={cn('capitalize', severityColors[issue.severity])}>
                          {issue.severity}
                        </Badge>
                      </TableCell>
                      <TableCell className="max-w-xs">
                        <p className="text-sm text-muted-foreground truncate">
                          {issue.description}
                        </p>
                        {issue.recommendation && (
                          <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                            Tip: {issue.recommendation}
                          </p>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleResolve(issue.id)}
                          disabled={resolveIssue.isPending}
                        >
                          <Check className="h-4 w-4 mr-1" />
                          Resolve
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Scan History */}
      {history && history.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Scan History</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {history.map((scan) => (
                <div
                  key={scan.id}
                  className="flex items-center justify-between p-2 rounded hover:bg-muted"
                >
                  <div className="flex items-center gap-3">
                    {scan.status === 'running' ? (
                      <Loader2 className="h-4 w-4 animate-spin text-blue-500" />
                    ) : scan.status === 'completed' ? (
                      <Check className="h-4 w-4 text-green-500" />
                    ) : (
                      <AlertTriangle className="h-4 w-4 text-red-500" />
                    )}
                    <div>
                      <p className="text-sm font-medium">
                        {format(new Date(scan.scanStartedAt), 'PPp')}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {scan.totalIssueCount} issues found
                      </p>
                    </div>
                  </div>
                  <span className="text-xs text-muted-foreground">
                    {formatDistanceToNow(new Date(scan.scanStartedAt), { addSuffix: true })}
                  </span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
