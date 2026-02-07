'use client';

import { SpaceCover } from './space-cover';
import { SpacePagesList } from './space-pages-list';
import { ChildSpacesList } from './child-spaces-list';
import { WidgetRenderer } from '@/features/pages/widgets';
import type { WidgetBlock } from '@/features/pages/widgets';

interface Page {
  id: string;
  title: string;
  slug: string;
  icon?: string;
  description?: string;
}

interface ChildSpace {
  id: string;
  name: string;
  slug: string;
  description?: string;
  iconUrl?: string;
}

interface Space {
  id: string;
  name: string;
  slug: string;
  description?: string;
  coverImageUrl?: string;
  homepageContent?: WidgetBlock[];
  pages: Page[];
  childSpaces: ChildSpace[];
}

interface SpaceLandingProps {
  space: Space;
}

export function SpaceLanding({ space }: SpaceLandingProps) {
  return (
    <div className="max-w-5xl mx-auto">
      {/* Cover Image */}
      <SpaceCover
        imageUrl={space.coverImageUrl}
        name={space.name}
        description={space.description}
      />

      {/* Homepage Content Widgets */}
      {space.homepageContent && space.homepageContent.length > 0 && (
        <div className="mb-8">
          <WidgetRenderer widgets={space.homepageContent} />
        </div>
      )}

      {/* Two Column Layout for Pages and Child Spaces */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Pages List */}
        {space.pages.length > 0 && (
          <SpacePagesList
            pages={space.pages}
            spaceSlug={space.slug}
          />
        )}

        {/* Child Spaces */}
        {space.childSpaces.length > 0 && (
          <ChildSpacesList spaces={space.childSpaces} />
        )}
      </div>

      {/* Full Width if only one section has content */}
      {space.pages.length > 0 && space.childSpaces.length === 0 && (
        <div className="lg:hidden">
          {/* Already rendered above in grid */}
        </div>
      )}
    </div>
  );
}
