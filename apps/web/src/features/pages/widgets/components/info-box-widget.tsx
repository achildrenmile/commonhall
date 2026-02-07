'use client';

import { Info, AlertTriangle, CheckCircle, XCircle, Lightbulb } from 'lucide-react';
import type { WidgetProps, InfoBoxData } from '../types';
import { cn } from '@/lib/utils';

const variantConfig = {
  info: {
    icon: Info,
    containerClass: 'border-blue-200 bg-blue-50 dark:border-blue-900 dark:bg-blue-950',
    iconClass: 'text-blue-600 dark:text-blue-400',
    titleClass: 'text-blue-800 dark:text-blue-200',
    bodyClass: 'text-blue-700 dark:text-blue-300',
  },
  warning: {
    icon: AlertTriangle,
    containerClass: 'border-yellow-200 bg-yellow-50 dark:border-yellow-900 dark:bg-yellow-950',
    iconClass: 'text-yellow-600 dark:text-yellow-400',
    titleClass: 'text-yellow-800 dark:text-yellow-200',
    bodyClass: 'text-yellow-700 dark:text-yellow-300',
  },
  success: {
    icon: CheckCircle,
    containerClass: 'border-green-200 bg-green-50 dark:border-green-900 dark:bg-green-950',
    iconClass: 'text-green-600 dark:text-green-400',
    titleClass: 'text-green-800 dark:text-green-200',
    bodyClass: 'text-green-700 dark:text-green-300',
  },
  error: {
    icon: XCircle,
    containerClass: 'border-red-200 bg-red-50 dark:border-red-900 dark:bg-red-950',
    iconClass: 'text-red-600 dark:text-red-400',
    titleClass: 'text-red-800 dark:text-red-200',
    bodyClass: 'text-red-700 dark:text-red-300',
  },
  tip: {
    icon: Lightbulb,
    containerClass: 'border-purple-200 bg-purple-50 dark:border-purple-900 dark:bg-purple-950',
    iconClass: 'text-purple-600 dark:text-purple-400',
    titleClass: 'text-purple-800 dark:text-purple-200',
    bodyClass: 'text-purple-700 dark:text-purple-300',
  },
};

export default function InfoBoxWidget({ data }: WidgetProps<InfoBoxData>) {
  const config = variantConfig[data.variant] || variantConfig.info;
  const Icon = config.icon;

  return (
    <div className={cn('rounded-lg border p-4', config.containerClass)}>
      <div className="flex gap-3">
        <Icon className={cn('h-5 w-5 shrink-0 mt-0.5', config.iconClass)} />
        <div className="flex-1 min-w-0">
          {data.title && (
            <p className={cn('font-medium mb-1', config.titleClass)}>
              {data.title}
            </p>
          )}
          <p className={cn('text-sm', config.bodyClass)}>
            {data.body}
          </p>
        </div>
      </div>
    </div>
  );
}
