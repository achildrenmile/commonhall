'use client';

import { useMemo } from 'react';
import { WidgetRenderer } from '../widgets';
import { TableOfContents, extractHeadings } from './table-of-contents';
import { Breadcrumb } from '@/components/layout/breadcrumb';
import type { WidgetBlock } from '../widgets';

interface Page {
  id: string;
  title: string;
  slug: string;
  content: WidgetBlock[];
  space?: {
    id: string;
    name: string;
    slug: string;
  };
  parent?: {
    id: string;
    title: string;
    slug: string;
  };
}

interface PageViewerProps {
  page: Page;
}

export function PageViewer({ page }: PageViewerProps) {
  const headings = useMemo(() => {
    return extractHeadings(page.content as Array<{ type: string; data: { html?: string } }>);
  }, [page.content]);

  const showToc = headings.length > 2;

  // Build breadcrumb items
  const breadcrumbItems = useMemo(() => {
    const items: Array<{ label: string; href?: string }> = [];

    if (page.space) {
      items.push({
        label: page.space.name,
        href: `/spaces/${page.space.slug}`,
      });
    }

    if (page.parent) {
      items.push({
        label: page.parent.title,
        href: page.space
          ? `/spaces/${page.space.slug}/${page.parent.slug}`
          : undefined,
      });
    }

    items.push({
      label: page.title,
    });

    return items;
  }, [page]);

  return (
    <div className="max-w-7xl mx-auto">
      {/* Breadcrumb */}
      <div className="mb-6">
        <Breadcrumb items={breadcrumbItems} />
      </div>

      {/* Title */}
      <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-8">
        {page.title}
      </h1>

      {/* Content with optional TOC */}
      <div className={showToc ? 'lg:grid lg:grid-cols-[1fr_220px] lg:gap-10' : ''}>
        {/* Main Content */}
        <div className="min-w-0">
          <WidgetRenderer widgets={page.content} />
        </div>

        {/* Table of Contents (sticky sidebar) */}
        {showToc && (
          <aside className="hidden lg:block">
            <div className="sticky top-24">
              <TableOfContents headings={headings} />
            </div>
          </aside>
        )}
      </div>
    </div>
  );
}
