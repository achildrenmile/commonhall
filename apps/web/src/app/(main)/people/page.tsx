'use client';

import { Suspense, useState, useCallback, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Search, LayoutGrid, List } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group';
import {
  PeopleFiltersSidebar,
  PeopleFiltersSheet,
  PeopleGrid,
  PersonGridSkeleton,
} from '@/features/people';
import { useDebounce } from '@/lib/hooks/use-debounce';

function PeopleContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Read initial state from URL
  const initialQuery = searchParams.get('q') || '';
  const initialDepartments = searchParams.get('department')?.split(',').filter(Boolean) || [];
  const initialLocations = searchParams.get('location')?.split(',').filter(Boolean) || [];
  const initialView = (searchParams.get('view') as 'grid' | 'list') || 'grid';

  // Local state
  const [searchInput, setSearchInput] = useState(initialQuery);
  const [selectedDepartments, setSelectedDepartments] = useState<string[]>(initialDepartments);
  const [selectedLocations, setSelectedLocations] = useState<string[]>(initialLocations);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>(initialView);

  // Debounce search input
  const debouncedQuery = useDebounce(searchInput, 300);

  // Sync state to URL
  const updateUrl = useCallback(() => {
    const params = new URLSearchParams();
    if (debouncedQuery) params.set('q', debouncedQuery);
    if (selectedDepartments.length) params.set('department', selectedDepartments.join(','));
    if (selectedLocations.length) params.set('location', selectedLocations.join(','));
    if (viewMode !== 'grid') params.set('view', viewMode);

    const newUrl = params.toString() ? `/people?${params.toString()}` : '/people';
    router.replace(newUrl, { scroll: false });
  }, [debouncedQuery, selectedDepartments, selectedLocations, viewMode, router]);

  useEffect(() => {
    updateUrl();
  }, [updateUrl]);

  const handleClearAll = useCallback(() => {
    setSearchInput('');
    setSelectedDepartments([]);
    setSelectedLocations([]);
  }, []);

  const filters = {
    query: debouncedQuery || undefined,
    departments: selectedDepartments.length > 0 ? selectedDepartments : undefined,
    locations: selectedLocations.length > 0 ? selectedLocations : undefined,
  };

  return (
    <div className="flex gap-6">
      {/* Desktop Filter Sidebar */}
      <PeopleFiltersSidebar
        selectedDepartments={selectedDepartments}
        selectedLocations={selectedLocations}
        onDepartmentsChange={setSelectedDepartments}
        onLocationsChange={setSelectedLocations}
        onClearAll={handleClearAll}
      />

      {/* Main Content */}
      <div className="flex-1 min-w-0">
        {/* Search & Controls */}
        <div className="flex flex-col sm:flex-row gap-4 mb-6">
          {/* Search Bar */}
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              type="text"
              placeholder="Search by name, email, title, or department..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="pl-10 h-12 text-base"
            />
          </div>

          {/* Mobile Filters & View Toggle */}
          <div className="flex items-center gap-2">
            <PeopleFiltersSheet
              selectedDepartments={selectedDepartments}
              selectedLocations={selectedLocations}
              onDepartmentsChange={setSelectedDepartments}
              onLocationsChange={setSelectedLocations}
              onClearAll={handleClearAll}
            />

            <ToggleGroup
              type="single"
              value={viewMode}
              onValueChange={(v) => v && setViewMode(v as 'grid' | 'list')}
              className="border rounded-md"
            >
              <ToggleGroupItem value="grid" aria-label="Grid view" className="px-3">
                <LayoutGrid className="h-4 w-4" />
              </ToggleGroupItem>
              <ToggleGroupItem value="list" aria-label="List view" className="px-3">
                <List className="h-4 w-4" />
              </ToggleGroupItem>
            </ToggleGroup>
          </div>
        </div>

        {/* Results */}
        <PeopleGrid filters={filters} viewMode={viewMode} />
      </div>
    </div>
  );
}

export default function PeoplePage() {
  return (
    <div className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100 mb-2">
          People
        </h1>
        <p className="text-slate-600 dark:text-slate-400">
          Find colleagues and browse the organization directory
        </p>
      </div>

      <Suspense fallback={<PersonGridSkeleton />}>
        <PeopleContent />
      </Suspense>
    </div>
  );
}
