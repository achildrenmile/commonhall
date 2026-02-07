'use client';

import { useMemo } from 'react';
import DOMPurify from 'dompurify';
import type { WidgetProps, RichTextData } from '../types';

function generateHeadingId(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim();
}

function addHeadingIds(html: string): string {
  // Add IDs to h1-h6 elements for anchor linking
  return html.replace(
    /<(h[1-6])([^>]*)>([^<]+)<\/h[1-6]>/gi,
    (match, tag, attrs, content) => {
      // Skip if already has an id
      if (/id=["'][^"']+["']/.test(attrs)) {
        return match;
      }
      const id = generateHeadingId(content);
      return `<${tag}${attrs} id="${id}">${content}</${tag}>`;
    }
  );
}

export default function RichTextWidget({ data }: WidgetProps<RichTextData>) {
  const sanitizedHtml = useMemo(() => {
    if (!data.html) return '';

    // Sanitize and add heading IDs
    const clean = DOMPurify.sanitize(data.html, {
      ADD_ATTR: ['target', 'rel'],
      ADD_TAGS: ['iframe'],
      FORBID_TAGS: ['script', 'style'],
    });

    return addHeadingIds(clean);
  }, [data.html]);

  if (!sanitizedHtml) {
    return null;
  }

  return (
    <div
      className="prose prose-slate dark:prose-invert max-w-none prose-headings:scroll-mt-20 prose-a:text-blue-600 dark:prose-a:text-blue-400 prose-img:rounded-lg"
      dangerouslySetInnerHTML={{ __html: sanitizedHtml }}
    />
  );
}
