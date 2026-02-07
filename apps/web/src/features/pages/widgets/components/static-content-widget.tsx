'use client';

import { useMemo } from 'react';
import DOMPurify from 'dompurify';
import type { WidgetProps, StaticContentData } from '../types';

export default function StaticContentWidget({ data }: WidgetProps<StaticContentData>) {
  const sanitizedHtml = useMemo(() => {
    if (!data.html) return '';
    return DOMPurify.sanitize(data.html, {
      FORBID_TAGS: ['script', 'style'],
    });
  }, [data.html]);

  if (!sanitizedHtml) {
    return null;
  }

  const style: React.CSSProperties = {};
  if (data.backgroundColor) style.backgroundColor = data.backgroundColor;
  if (data.textColor) style.color = data.textColor;

  return (
    <div
      className="rounded-lg p-6"
      style={style}
      dangerouslySetInnerHTML={{ __html: sanitizedHtml }}
    />
  );
}
