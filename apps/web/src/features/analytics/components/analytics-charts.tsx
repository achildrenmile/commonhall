'use client';

import {
  LineChart,
  Line,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import { format, parseISO } from 'date-fns';
import type { DailyCount, ContentRanking, SearchQueryRanking, DeviceDistribution } from '../types';

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

interface DailyChartProps {
  data: DailyCount[];
  secondaryData?: DailyCount[];
  primaryLabel: string;
  secondaryLabel?: string;
}

export function DailyChart({ data, secondaryData, primaryLabel, secondaryLabel }: DailyChartProps) {
  const chartData = data.map((item, index) => ({
    date: item.date,
    [primaryLabel]: item.count,
    ...(secondaryData?.[index] ? { [secondaryLabel!]: secondaryData[index].count } : {}),
  }));

  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
        <XAxis
          dataKey="date"
          tickFormatter={(value) => format(parseISO(value), 'MMM d')}
          className="text-xs"
        />
        <YAxis className="text-xs" />
        <Tooltip
          labelFormatter={(value) => format(parseISO(value as string), 'MMM d, yyyy')}
          contentStyle={{
            backgroundColor: 'hsl(var(--card))',
            border: '1px solid hsl(var(--border))',
            borderRadius: '6px',
          }}
        />
        <Legend />
        <Line
          type="monotone"
          dataKey={primaryLabel}
          stroke="#3b82f6"
          strokeWidth={2}
          dot={false}
          activeDot={{ r: 6 }}
        />
        {secondaryData && secondaryLabel && (
          <Line
            type="monotone"
            dataKey={secondaryLabel}
            stroke="#10b981"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 6 }}
          />
        )}
      </LineChart>
    </ResponsiveContainer>
  );
}

interface TopContentChartProps {
  data: ContentRanking[];
  title: string;
}

export function TopContentChart({ data, title }: TopContentChartProps) {
  const chartData = data.slice(0, 10).map((item) => ({
    name: item.title.length > 30 ? `${item.title.slice(0, 30)}...` : item.title,
    views: item.views,
    unique: item.uniqueViewers,
  }));

  return (
    <div>
      <h3 className="font-medium mb-4">{title}</h3>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData} layout="vertical" margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
          <XAxis type="number" className="text-xs" />
          <YAxis type="category" dataKey="name" width={150} className="text-xs" />
          <Tooltip
            contentStyle={{
              backgroundColor: 'hsl(var(--card))',
              border: '1px solid hsl(var(--border))',
              borderRadius: '6px',
            }}
          />
          <Bar dataKey="views" fill="#3b82f6" name="Total Views" />
          <Bar dataKey="unique" fill="#10b981" name="Unique Viewers" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

interface SearchQueriesChartProps {
  data: SearchQueryRanking[];
  title: string;
}

export function SearchQueriesChart({ data, title }: SearchQueriesChartProps) {
  const chartData = data.slice(0, 10).map((item) => ({
    query: item.query.length > 25 ? `${item.query.slice(0, 25)}...` : item.query,
    count: item.count,
    avgResults: item.resultCount,
  }));

  return (
    <div>
      <h3 className="font-medium mb-4">{title}</h3>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData} layout="vertical" margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
          <XAxis type="number" className="text-xs" />
          <YAxis type="category" dataKey="query" width={150} className="text-xs" />
          <Tooltip
            contentStyle={{
              backgroundColor: 'hsl(var(--card))',
              border: '1px solid hsl(var(--border))',
              borderRadius: '6px',
            }}
          />
          <Bar dataKey="count" fill="#8b5cf6" name="Searches" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

interface DeviceChartProps {
  data: DeviceDistribution[];
}

export function DeviceChart({ data }: DeviceChartProps) {
  const chartData = data.map((item) => ({
    name: item.deviceType.charAt(0).toUpperCase() + item.deviceType.slice(1),
    value: item.count,
  }));

  const total = chartData.reduce((sum, item) => sum + item.value, 0);

  return (
    <div>
      <h3 className="font-medium mb-4">Device Distribution</h3>
      <ResponsiveContainer width="100%" height={250}>
        <PieChart>
          <Pie
            data={chartData}
            cx="50%"
            cy="50%"
            innerRadius={60}
            outerRadius={80}
            paddingAngle={5}
            dataKey="value"
            label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
          >
            {chartData.map((_, index) => (
              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip
            formatter={(value: number) => [`${value} (${((value / total) * 100).toFixed(1)}%)`, 'Count']}
            contentStyle={{
              backgroundColor: 'hsl(var(--card))',
              border: '1px solid hsl(var(--border))',
              borderRadius: '6px',
            }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}
