'use client';

import { useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { Plus, Users, Lock, Globe, Search, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useCommunities, useJoinCommunity } from '../api';
import type { CommunityListItem, CommunityType } from '../types';
import { useToast } from '@/hooks/use-toast';
import { useDebounce } from '@/hooks/use-debounce';

const typeIcons: Record<CommunityType, React.ElementType> = {
  Open: Globe,
  Closed: Lock,
  Assigned: Users,
};

function CommunityCard({ community }: { community: CommunityListItem }) {
  const { toast } = useToast();
  const joinMutation = useJoinCommunity();
  const TypeIcon = typeIcons[community.type];

  const handleJoin = async (e: React.MouseEvent) => {
    e.preventDefault();
    try {
      await joinMutation.mutateAsync(community.slug);
      toast({ title: 'Joined community' });
    } catch {
      toast({ title: 'Error', description: 'Failed to join community', variant: 'destructive' });
    }
  };

  return (
    <Link
      href={`/communities/${community.slug}`}
      className="group block rounded-lg border bg-card hover:border-foreground/20 transition-colors overflow-hidden"
    >
      <div className="relative h-32 bg-gradient-to-r from-blue-500 to-purple-500">
        {community.coverImageUrl && (
          <Image
            src={community.coverImageUrl}
            alt={community.name}
            fill
            className="object-cover"
          />
        )}
      </div>
      <div className="p-4">
        <div className="flex items-start justify-between gap-2 mb-2">
          <h3 className="font-semibold group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors line-clamp-1">
            {community.name}
          </h3>
          <Badge variant="outline" className="shrink-0">
            <TypeIcon className="h-3 w-3 mr-1" />
            {community.type}
          </Badge>
        </div>
        {community.description && (
          <p className="text-sm text-muted-foreground line-clamp-2 mb-3">
            {community.description}
          </p>
        )}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1 text-sm text-muted-foreground">
            <Users className="h-4 w-4" />
            {community.memberCount} members
          </div>
          {community.isMember ? (
            <Badge variant="secondary">Member</Badge>
          ) : community.type === 'Open' ? (
            <Button
              size="sm"
              onClick={handleJoin}
              disabled={joinMutation.isPending}
            >
              {joinMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                'Join'
              )}
            </Button>
          ) : null}
        </div>
      </div>
    </Link>
  );
}

export function CommunityList() {
  const [tab, setTab] = useState<'discover' | 'my'>('discover');
  const [searchInput, setSearchInput] = useState('');
  const search = useDebounce(searchInput, 300);

  const { data: communities, isLoading } = useCommunities(
    search || undefined,
    tab === 'my'
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Communities</h1>
          <p className="text-sm text-muted-foreground">
            Connect with colleagues around shared interests
          </p>
        </div>
        <Button asChild>
          <Link href="/communities/new">
            <Plus className="h-4 w-4 mr-2" />
            Create Community
          </Link>
        </Button>
      </div>

      <div className="flex flex-col sm:flex-row gap-4">
        <Tabs value={tab} onValueChange={(v) => setTab(v as 'discover' | 'my')}>
          <TabsList>
            <TabsTrigger value="discover">Discover</TabsTrigger>
            <TabsTrigger value="my">My Communities</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search communities..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : !communities?.length ? (
        <div className="text-center py-16 bg-muted/30 rounded-lg border border-dashed">
          <Users className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="font-semibold mb-2">
            {tab === 'my' ? 'No communities yet' : 'No communities found'}
          </h3>
          <p className="text-sm text-muted-foreground mb-4">
            {tab === 'my'
              ? 'Join or create a community to get started'
              : 'Try a different search term'}
          </p>
          {tab === 'my' && (
            <Button asChild>
              <Link href="/communities/new">
                <Plus className="h-4 w-4 mr-2" />
                Create Community
              </Link>
            </Button>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {communities.map((community) => (
            <CommunityCard key={community.id} community={community} />
          ))}
        </div>
      )}
    </div>
  );
}
