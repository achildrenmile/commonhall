'use client';

import { Smile, Meh, Frown } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';

interface SentimentBadgeProps {
  sentiment: 'positive' | 'neutral' | 'negative' | string | null | undefined;
  score?: number;
  showLabel?: boolean;
  className?: string;
}

const sentimentConfig: Record<string, {
  icon: React.ElementType;
  label: string;
  color: string;
  bgColor: string;
}> = {
  positive: {
    icon: Smile,
    label: 'Positive',
    color: 'text-green-600 dark:text-green-400',
    bgColor: 'bg-green-100 dark:bg-green-900/30',
  },
  neutral: {
    icon: Meh,
    label: 'Neutral',
    color: 'text-slate-600 dark:text-slate-400',
    bgColor: 'bg-slate-100 dark:bg-slate-800',
  },
  negative: {
    icon: Frown,
    label: 'Negative',
    color: 'text-red-600 dark:text-red-400',
    bgColor: 'bg-red-100 dark:bg-red-900/30',
  },
};

export function SentimentBadge({
  sentiment,
  score,
  showLabel = false,
  className,
}: SentimentBadgeProps) {
  if (!sentiment) return null;

  const config = sentimentConfig[sentiment.toLowerCase()] || sentimentConfig.neutral;
  const Icon = config.icon;
  const confidencePercent = score ? Math.round(score * 100) : null;

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge
            variant="secondary"
            className={cn(
              'gap-1',
              config.bgColor,
              config.color,
              className
            )}
          >
            <Icon className="h-3 w-3" />
            {showLabel && <span>{config.label}</span>}
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <p>
            Sentiment: <strong>{config.label}</strong>
            {confidencePercent !== null && (
              <span className="text-muted-foreground ml-1">
                ({confidencePercent}% confidence)
              </span>
            )}
          </p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}
