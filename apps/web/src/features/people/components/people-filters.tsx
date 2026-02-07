'use client';

import { X, Filter } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/sheet';
import { ScrollArea } from '@/components/ui/scroll-area';
import { usePeopleFacets, type FacetItem } from '../api';

interface PeopleFiltersProps {
  selectedDepartments: string[];
  selectedLocations: string[];
  onDepartmentsChange: (departments: string[]) => void;
  onLocationsChange: (locations: string[]) => void;
  onClearAll: () => void;
}

function FilterCheckboxList({
  title,
  items,
  selected,
  onChange,
  isLoading,
}: {
  title: string;
  items: FacetItem[];
  selected: string[];
  onChange: (values: string[]) => void;
  isLoading: boolean;
}) {
  const toggleItem = (name: string) => {
    if (selected.includes(name)) {
      onChange(selected.filter((s) => s !== name));
    } else {
      onChange([...selected, name]);
    }
  };

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
        {title}
      </h3>
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="flex items-center gap-2">
              <Skeleton className="h-4 w-4 rounded" />
              <Skeleton className="h-4 w-24" />
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <p className="text-sm text-slate-500">No {title.toLowerCase()} available</p>
      ) : (
        <div className="space-y-2">
          {items.map((item) => (
            <div key={item.name} className="flex items-center gap-2">
              <Checkbox
                id={`${title}-${item.name}`}
                checked={selected.includes(item.name)}
                onCheckedChange={() => toggleItem(item.name)}
              />
              <Label
                htmlFor={`${title}-${item.name}`}
                className="text-sm text-slate-700 dark:text-slate-300 flex-1 cursor-pointer"
              >
                {item.name}
              </Label>
              <span className="text-xs text-slate-400">
                {item.count}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function FilterContent({
  selectedDepartments,
  selectedLocations,
  onDepartmentsChange,
  onLocationsChange,
  onClearAll,
}: PeopleFiltersProps) {
  const { data: facets, isLoading } = usePeopleFacets();

  const hasFilters = selectedDepartments.length > 0 || selectedLocations.length > 0;

  return (
    <div className="space-y-6">
      {hasFilters && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onClearAll}
          className="w-full justify-start gap-2 text-slate-500 hover:text-slate-900 dark:hover:text-slate-100"
        >
          <X className="h-4 w-4" />
          Clear all filters
        </Button>
      )}

      <FilterCheckboxList
        title="Department"
        items={facets?.departments || []}
        selected={selectedDepartments}
        onChange={onDepartmentsChange}
        isLoading={isLoading}
      />

      <FilterCheckboxList
        title="Location"
        items={facets?.locations || []}
        selected={selectedLocations}
        onChange={onLocationsChange}
        isLoading={isLoading}
      />
    </div>
  );
}

// Desktop Sidebar
export function PeopleFiltersSidebar(props: PeopleFiltersProps) {
  return (
    <aside className="hidden lg:block w-64 shrink-0">
      <div className="sticky top-24 rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-950 p-4">
        <h2 className="font-semibold text-slate-900 dark:text-slate-100 mb-4">
          Filters
        </h2>
        <ScrollArea className="h-[calc(100vh-200px)]">
          <FilterContent {...props} />
        </ScrollArea>
      </div>
    </aside>
  );
}

// Mobile Sheet
export function PeopleFiltersSheet(props: PeopleFiltersProps) {
  const hasFilters = props.selectedDepartments.length > 0 || props.selectedLocations.length > 0;

  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="outline" size="sm" className="lg:hidden gap-2">
          <Filter className="h-4 w-4" />
          Filters
          {hasFilters && (
            <span className="ml-1 rounded-full bg-blue-500 text-white text-xs h-5 w-5 flex items-center justify-center">
              {props.selectedDepartments.length + props.selectedLocations.length}
            </span>
          )}
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-80">
        <SheetHeader>
          <SheetTitle>Filters</SheetTitle>
        </SheetHeader>
        <div className="mt-6">
          <FilterContent {...props} />
        </div>
      </SheetContent>
    </Sheet>
  );
}
