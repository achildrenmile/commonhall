'use client';

import { useState, useMemo } from 'react';
import Image from 'next/image';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  Users,
  Lock,
  Globe,
  Settings,
  LogOut,
  Loader2,
  MessageSquare,
  Heart,
  Pin,
  MoreHorizontal,
  Send,
  ChevronDown,
  Info,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useToast } from '@/hooks/use-toast';
import { formatRelativeTime } from '@/lib/utils';
import {
  useCommunity,
  useCommunityPosts,
  useCommunityMembers,
  useJoinCommunity,
  useLeaveCommunity,
  useCreatePost,
  useLikePost,
} from '../api';
import { PostComments } from './post-comments';
import type { CommunityPost, CommunityType } from '../types';

interface CommunityDetailProps {
  slug: string;
}

const typeIcons: Record<CommunityType, React.ElementType> = {
  Open: Globe,
  Closed: Lock,
  Assigned: Users,
};

function PostCard({ post, slug }: { post: CommunityPost; slug: string }) {
  const [showComments, setShowComments] = useState(false);
  const likeMutation = useLikePost();

  const handleLike = async () => {
    try {
      await likeMutation.mutateAsync({ slug, postId: post.id });
    } catch {
      // Ignore
    }
  };

  const authorName = [post.authorFirstName, post.authorLastName].filter(Boolean).join(' ') || 'Unknown';
  const initials = `${post.authorFirstName?.[0] || ''}${post.authorLastName?.[0] || ''}`;

  return (
    <div className="border rounded-lg p-4 bg-card">
      <div className="flex items-start gap-3">
        <Avatar className="h-10 w-10">
          <AvatarImage src={post.authorProfilePhotoUrl || undefined} />
          <AvatarFallback>{initials || '?'}</AvatarFallback>
        </Avatar>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className="font-medium">{authorName}</span>
            <span className="text-sm text-muted-foreground">
              {formatRelativeTime(post.createdAt)}
            </span>
            {post.isPinned && (
              <Pin className="h-3 w-3 text-blue-500" />
            )}
          </div>
          <p className="whitespace-pre-wrap break-words">{post.body}</p>
          {post.imageUrl && (
            <div className="relative mt-3 rounded-lg overflow-hidden max-h-96">
              <Image
                src={post.imageUrl}
                alt="Post image"
                width={600}
                height={400}
                className="object-cover w-full"
              />
            </div>
          )}
          <div className="flex items-center gap-4 mt-3 text-sm">
            <button
              onClick={handleLike}
              className={`flex items-center gap-1 hover:text-red-500 transition-colors ${
                post.hasLiked ? 'text-red-500' : 'text-muted-foreground'
              }`}
            >
              <Heart className={`h-4 w-4 ${post.hasLiked ? 'fill-current' : ''}`} />
              {post.likeCount}
            </button>
            <button
              onClick={() => setShowComments(!showComments)}
              className="flex items-center gap-1 text-muted-foreground hover:text-foreground transition-colors"
            >
              <MessageSquare className="h-4 w-4" />
              {post.commentCount}
            </button>
          </div>
          {showComments && (
            <div className="mt-4 pt-4 border-t">
              <PostComments slug={slug} postId={post.id} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function PostComposer({ slug, canPost }: { slug: string; canPost: boolean }) {
  const [body, setBody] = useState('');
  const createMutation = useCreatePost();
  const { toast } = useToast();

  const handleSubmit = async () => {
    if (!body.trim()) return;

    try {
      await createMutation.mutateAsync({ slug, body });
      setBody('');
      toast({ title: 'Post created' });
    } catch {
      toast({ title: 'Error', description: 'Failed to create post', variant: 'destructive' });
    }
  };

  if (!canPost) return null;

  return (
    <div className="border rounded-lg p-4 bg-card">
      <Textarea
        placeholder="Share something with the community..."
        value={body}
        onChange={(e) => setBody(e.target.value)}
        rows={3}
      />
      <div className="flex justify-end mt-3">
        <Button
          onClick={handleSubmit}
          disabled={!body.trim() || createMutation.isPending}
        >
          {createMutation.isPending ? (
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
          ) : (
            <Send className="h-4 w-4 mr-2" />
          )}
          Post
        </Button>
      </div>
    </div>
  );
}

function PostsTab({ slug, canPost }: { slug: string; canPost: boolean }) {
  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useCommunityPosts(slug);

  const posts = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  return (
    <div className="space-y-4">
      <PostComposer slug={slug} canPost={canPost} />

      {isLoading ? (
        <div className="flex justify-center py-8">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : posts.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground">
          No posts yet. Be the first to share something!
        </div>
      ) : (
        <>
          {posts.map((post) => (
            <PostCard key={post.id} post={post} slug={slug} />
          ))}
          {hasNextPage && (
            <div className="text-center">
              <Button
                variant="outline"
                onClick={() => fetchNextPage()}
                disabled={isFetchingNextPage}
              >
                {isFetchingNextPage ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : (
                  <ChevronDown className="h-4 w-4 mr-2" />
                )}
                Load More
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function MembersTab({ slug }: { slug: string }) {
  const { data, isLoading, fetchNextPage, hasNextPage, isFetchingNextPage } = useCommunityMembers(slug);

  const members = useMemo(() => {
    return data?.pages.flatMap((page) => page.items) || [];
  }, [data]);

  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {members.map((member) => {
        const name = [member.firstName, member.lastName].filter(Boolean).join(' ') || 'Unknown';
        const initials = `${member.firstName?.[0] || ''}${member.lastName?.[0] || ''}`;

        return (
          <div key={member.id} className="flex items-center gap-3 p-3 border rounded-lg">
            <Avatar>
              <AvatarImage src={member.profilePhotoUrl || undefined} />
              <AvatarFallback>{initials || '?'}</AvatarFallback>
            </Avatar>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-medium">{name}</span>
                {member.role !== 'Member' && (
                  <Badge variant="secondary">{member.role}</Badge>
                )}
              </div>
              <p className="text-sm text-muted-foreground truncate">{member.email}</p>
            </div>
            <Button variant="ghost" size="sm" asChild>
              <Link href={`/directory/${member.userId}`}>View Profile</Link>
            </Button>
          </div>
        );
      })}

      {hasNextPage && (
        <div className="text-center pt-4">
          <Button
            variant="outline"
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
          >
            {isFetchingNextPage ? 'Loading...' : 'Load More'}
          </Button>
        </div>
      )}
    </div>
  );
}

function AboutTab({ community }: { community: NonNullable<ReturnType<typeof useCommunity>['data']> }) {
  const TypeIcon = typeIcons[community.type];

  return (
    <div className="space-y-4">
      <div className="border rounded-lg p-4">
        <h3 className="font-semibold mb-2">About</h3>
        <p className="text-muted-foreground">
          {community.description || 'No description available.'}
        </p>
      </div>

      <div className="border rounded-lg p-4">
        <h3 className="font-semibold mb-2">Details</h3>
        <dl className="space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Type</dt>
            <dd className="flex items-center gap-1">
              <TypeIcon className="h-4 w-4" />
              {community.type}
            </dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Members</dt>
            <dd>{community.memberCount}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Who can post</dt>
            <dd>{community.postPermission}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Created</dt>
            <dd>{formatRelativeTime(community.createdAt)}</dd>
          </div>
        </dl>
      </div>
    </div>
  );
}

export function CommunityDetail({ slug }: CommunityDetailProps) {
  const router = useRouter();
  const { toast } = useToast();
  const { data: community, isLoading } = useCommunity(slug);
  const joinMutation = useJoinCommunity();
  const leaveMutation = useLeaveCommunity();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!community) {
    return (
      <div className="text-center py-16">
        <h2 className="text-xl font-semibold mb-2">Community not found</h2>
        <Button variant="outline" onClick={() => router.push('/communities')}>
          Back to Communities
        </Button>
      </div>
    );
  }

  if (community.isRestricted) {
    return (
      <div className="text-center py-16">
        <Lock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
        <h2 className="text-xl font-semibold mb-2">{community.name}</h2>
        <p className="text-muted-foreground mb-4">
          This is a closed community. You need an invitation to join.
        </p>
        <Button variant="outline" onClick={() => router.push('/communities')}>
          Back to Communities
        </Button>
      </div>
    );
  }

  const handleJoin = async () => {
    try {
      await joinMutation.mutateAsync(slug);
      toast({ title: 'Joined community' });
    } catch {
      toast({ title: 'Error', description: 'Failed to join', variant: 'destructive' });
    }
  };

  const handleLeave = async () => {
    try {
      await leaveMutation.mutateAsync(slug);
      toast({ title: 'Left community' });
    } catch (error: any) {
      toast({ title: 'Error', description: error.message || 'Failed to leave', variant: 'destructive' });
    }
  };

  const TypeIcon = typeIcons[community.type];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="relative h-48 sm:h-64 bg-gradient-to-r from-blue-500 to-purple-500 rounded-lg overflow-hidden">
        {community.coverImageUrl && (
          <Image
            src={community.coverImageUrl}
            alt={community.name}
            fill
            className="object-cover"
          />
        )}
        <div className="absolute inset-0 bg-black/30" />
        <div className="absolute bottom-4 left-4 right-4">
          <div className="flex items-end justify-between">
            <div>
              <div className="flex items-center gap-2 mb-1">
                <h1 className="text-2xl sm:text-3xl font-bold text-white">
                  {community.name}
                </h1>
                <Badge variant="secondary" className="bg-white/20 text-white border-0">
                  <TypeIcon className="h-3 w-3 mr-1" />
                  {community.type}
                </Badge>
              </div>
              <div className="flex items-center gap-4 text-white/80 text-sm">
                <span className="flex items-center gap-1">
                  <Users className="h-4 w-4" />
                  {community.memberCount} members
                </span>
              </div>
            </div>
            <div className="flex gap-2">
              {community.isMember ? (
                <>
                  {community.myRole === 'Admin' && (
                    <Button variant="secondary" size="sm" asChild>
                      <Link href={`/studio/communities/${slug}`}>
                        <Settings className="h-4 w-4 mr-2" />
                        Manage
                      </Link>
                    </Button>
                  )}
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="secondary" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem
                        onClick={handleLeave}
                        disabled={leaveMutation.isPending}
                      >
                        <LogOut className="h-4 w-4 mr-2" />
                        Leave Community
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </>
              ) : community.canJoin ? (
                <Button onClick={handleJoin} disabled={joinMutation.isPending}>
                  {joinMutation.isPending ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : null}
                  Join Community
                </Button>
              ) : null}
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="posts">
        <TabsList>
          <TabsTrigger value="posts">Posts</TabsTrigger>
          <TabsTrigger value="members">Members</TabsTrigger>
          <TabsTrigger value="about">About</TabsTrigger>
        </TabsList>
        <TabsContent value="posts" className="mt-6">
          <PostsTab slug={slug} canPost={community.canPost} />
        </TabsContent>
        <TabsContent value="members" className="mt-6">
          <MembersTab slug={slug} />
        </TabsContent>
        <TabsContent value="about" className="mt-6">
          <AboutTab community={community} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
