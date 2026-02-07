'use client';

import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import type { WidgetProps, ButtonData } from '../types';

export default function ButtonWidget({ data }: WidgetProps<ButtonData>) {
  const alignment = data.alignment || 'left';
  const variant = data.variant || 'default';

  const alignmentClass = {
    left: 'justify-start',
    center: 'justify-center',
    right: 'justify-end',
  }[alignment];

  const isExternal = data.url.startsWith('http://') || data.url.startsWith('https://');

  return (
    <div className={cn('flex', alignmentClass)}>
      {isExternal ? (
        <Button variant={variant} asChild>
          <a href={data.url} target="_blank" rel="noopener noreferrer">
            {data.text}
          </a>
        </Button>
      ) : (
        <Button variant={variant} asChild>
          <Link href={data.url}>
            {data.text}
          </Link>
        </Button>
      )}
    </div>
  );
}
