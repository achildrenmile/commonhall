'use client';

import { useState, useEffect } from 'react';
import { cn } from '@/lib/utils';

interface TocHeading {
  id: string;
  text: string;
  level: number;
}

interface TableOfContentsProps {
  headings: TocHeading[];
}

export function TableOfContents({ headings }: TableOfContentsProps) {
  const [activeId, setActiveId] = useState<string>('');

  useEffect(() => {
    if (headings.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setActiveId(entry.target.id);
          }
        });
      },
      {
        rootMargin: '-80px 0% -80% 0%',
        threshold: 0,
      }
    );

    headings.forEach(({ id }) => {
      const element = document.getElementById(id);
      if (element) {
        observer.observe(element);
      }
    });

    return () => {
      headings.forEach(({ id }) => {
        const element = document.getElementById(id);
        if (element) {
          observer.unobserve(element);
        }
      });
    };
  }, [headings]);

  if (headings.length === 0) {
    return null;
  }

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>, id: string) => {
    e.preventDefault();
    const element = document.getElementById(id);
    if (element) {
      const top = element.getBoundingClientRect().top + window.scrollY - 80;
      window.scrollTo({ top, behavior: 'smooth' });
      setActiveId(id);
    }
  };

  return (
    <nav className="space-y-1">
      <p className="text-xs font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-3">
        On this page
      </p>
      {headings.map((heading) => (
        <a
          key={heading.id}
          href={`#${heading.id}`}
          onClick={(e) => handleClick(e, heading.id)}
          className={cn(
            'block text-sm transition-colors hover:text-slate-900 dark:hover:text-slate-100',
            heading.level === 2 && 'pl-0',
            heading.level === 3 && 'pl-3',
            heading.level === 4 && 'pl-6',
            heading.level >= 5 && 'pl-9',
            activeId === heading.id
              ? 'text-blue-600 dark:text-blue-400 font-medium'
              : 'text-slate-600 dark:text-slate-400'
          )}
        >
          {heading.text}
        </a>
      ))}
    </nav>
  );
}

export function extractHeadings(widgets: Array<{ type: string; data: { html?: string } }>): TocHeading[] {
  const headings: TocHeading[] = [];

  widgets.forEach((widget) => {
    if ((widget.type === 'rich-text' || widget.type === 'static-content') && widget.data.html) {
      const headingRegex = /<h([2-6])(?:[^>]*id=["']([^"']+)["'])?[^>]*>([^<]+)<\/h[2-6]>/gi;
      let match;

      while ((match = headingRegex.exec(widget.data.html)) !== null) {
        const level = parseInt(match[1], 10);
        const id = match[2] || generateHeadingId(match[3]);
        const text = match[3].trim();

        if (text) {
          headings.push({ id, text, level });
        }
      }
    }
  });

  return headings;
}

function generateHeadingId(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim();
}
