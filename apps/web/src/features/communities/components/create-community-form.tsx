'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Loader2, Globe, Lock, Users, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import { useCreateCommunity } from '../api';
import type { CommunityType, CommunityPostPermission } from '../types';

const typeDescriptions: Record<CommunityType, { icon: React.ElementType; description: string }> = {
  Open: {
    icon: Globe,
    description: 'Anyone can view and join this community',
  },
  Closed: {
    icon: Lock,
    description: 'Invite-only. Members must be approved to join',
  },
  Assigned: {
    icon: Users,
    description: 'Members are automatically assigned based on groups',
  },
};

const postPermissionDescriptions: Record<CommunityPostPermission, string> = {
  Anyone: 'All community members can create posts',
  MembersOnly: 'Only approved members can post',
  AdminsOnly: 'Only admins and moderators can post',
};

export function CreateCommunityForm() {
  const router = useRouter();
  const { toast } = useToast();
  const createMutation = useCreateCommunity();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<CommunityType>('Open');
  const [postPermission, setPostPermission] = useState<CommunityPostPermission>('Anyone');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!name.trim()) {
      toast({ title: 'Error', description: 'Name is required', variant: 'destructive' });
      return;
    }

    try {
      const result = await createMutation.mutateAsync({
        name: name.trim(),
        description: description.trim() || undefined,
        type,
        postPermission,
      });
      toast({ title: 'Community created' });
      router.push(`/communities/${result.slug}`);
    } catch {
      toast({ title: 'Error', description: 'Failed to create community', variant: 'destructive' });
    }
  };

  const TypeIcon = typeDescriptions[type].icon;

  return (
    <div className="max-w-2xl mx-auto">
      <div className="mb-6">
        <Button variant="ghost" size="sm" onClick={() => router.back()}>
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back
        </Button>
      </div>

      <div className="border rounded-lg p-6 bg-card">
        <h1 className="text-2xl font-bold mb-2">Create Community</h1>
        <p className="text-sm text-muted-foreground mb-6">
          Build a space for people to connect around shared interests
        </p>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="name">Name *</Label>
            <Input
              id="name"
              placeholder="e.g., Engineering Team, Book Club"
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={100}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              placeholder="What is this community about?"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              maxLength={500}
            />
            <p className="text-xs text-muted-foreground">
              {description.length}/500 characters
            </p>
          </div>

          <div className="space-y-2">
            <Label>Community Type</Label>
            <Select value={type} onValueChange={(v) => setType(v as CommunityType)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(typeDescriptions).map(([key, { icon: Icon, description }]) => (
                  <SelectItem key={key} value={key}>
                    <div className="flex items-center gap-2">
                      <Icon className="h-4 w-4" />
                      <span>{key}</span>
                    </div>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <TypeIcon className="h-4 w-4" />
              {typeDescriptions[type].description}
            </div>
          </div>

          <div className="space-y-2">
            <Label>Who can post?</Label>
            <Select
              value={postPermission}
              onValueChange={(v) => setPostPermission(v as CommunityPostPermission)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Anyone">Anyone</SelectItem>
                <SelectItem value="MembersOnly">Members Only</SelectItem>
                <SelectItem value="AdminsOnly">Admins Only</SelectItem>
              </SelectContent>
            </Select>
            <p className="text-sm text-muted-foreground">
              {postPermissionDescriptions[postPermission]}
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button type="button" variant="outline" onClick={() => router.back()}>
              Cancel
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending && (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              )}
              Create Community
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
