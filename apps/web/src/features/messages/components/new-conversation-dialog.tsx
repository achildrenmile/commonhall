'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Loader2, Search, Check, X } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { useToast } from '@/hooks/use-toast';
import { useDebounce } from '@/lib/hooks/use-debounce';
import { apiClient } from '@/lib/api-client';
import { useQuery } from '@tanstack/react-query';
import { useCreateConversation } from '../api';
import type { ConversationType } from '../types';

interface NewConversationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface UserSearchResult {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  profilePhotoUrl?: string;
}

export function NewConversationDialog({ open, onOpenChange }: NewConversationDialogProps) {
  const router = useRouter();
  const { toast } = useToast();
  const [searchInput, setSearchInput] = useState('');
  const [selectedUsers, setSelectedUsers] = useState<UserSearchResult[]>([]);
  const [groupName, setGroupName] = useState('');
  const search = useDebounce(searchInput, 300);

  const createMutation = useCreateConversation();

  const { data: searchResults, isLoading: isSearching } = useQuery({
    queryKey: ['user-search', search],
    queryFn: async () => {
      if (!search || search.length < 2) return [];
      const params = new URLSearchParams({ query: search, limit: '10' });
      return apiClient.get<UserSearchResult[]>(`/users/search?${params.toString()}`);
    },
    enabled: search.length >= 2,
  });

  const filteredResults = searchResults?.filter(
    (user) => !selectedUsers.find((s) => s.id === user.id)
  );

  const handleSelectUser = (user: UserSearchResult) => {
    setSelectedUsers((prev) => [...prev, user]);
    setSearchInput('');
  };

  const handleRemoveUser = (userId: string) => {
    setSelectedUsers((prev) => prev.filter((u) => u.id !== userId));
  };

  const handleCreate = async () => {
    if (selectedUsers.length === 0) {
      toast({ title: 'Error', description: 'Select at least one participant', variant: 'destructive' });
      return;
    }

    const type: ConversationType = selectedUsers.length === 1 ? 'Direct' : 'Group';

    if (type === 'Group' && !groupName.trim()) {
      toast({ title: 'Error', description: 'Group name is required', variant: 'destructive' });
      return;
    }

    try {
      const result = await createMutation.mutateAsync({
        type,
        name: type === 'Group' ? groupName.trim() : undefined,
        participantUserIds: selectedUsers.map((u) => u.id),
      });

      onOpenChange(false);
      setSelectedUsers([]);
      setGroupName('');
      router.push(`/messages/${result.id}`);
    } catch {
      toast({ title: 'Error', description: 'Failed to create conversation', variant: 'destructive' });
    }
  };

  const handleClose = () => {
    onOpenChange(false);
    setSelectedUsers([]);
    setSearchInput('');
    setGroupName('');
  };

  const isGroup = selectedUsers.length > 1;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>New Conversation</DialogTitle>
          <DialogDescription>
            Start a direct message or create a group chat
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {/* Selected Users */}
          {selectedUsers.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {selectedUsers.map((user) => (
                <Badge key={user.id} variant="secondary" className="pl-2 pr-1 py-1">
                  {user.firstName} {user.lastName}
                  <button
                    onClick={() => handleRemoveUser(user.id)}
                    className="ml-1 hover:bg-muted rounded-full p-0.5"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
            </div>
          )}

          {/* Group Name (only for groups) */}
          {isGroup && (
            <div className="space-y-2">
              <Label htmlFor="groupName">Group Name</Label>
              <Input
                id="groupName"
                placeholder="Enter group name..."
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
              />
            </div>
          )}

          {/* User Search */}
          <div className="space-y-2">
            <Label>Add Participants</Label>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by name or email..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-9"
              />
            </div>
          </div>

          {/* Search Results */}
          {searchInput.length >= 2 && (
            <div className="border rounded-lg max-h-48 overflow-y-auto">
              {isSearching ? (
                <div className="flex items-center justify-center py-4">
                  <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                </div>
              ) : !filteredResults?.length ? (
                <p className="text-sm text-muted-foreground text-center py-4">
                  No users found
                </p>
              ) : (
                <div className="divide-y">
                  {filteredResults.map((user) => {
                    const name = `${user.firstName} ${user.lastName}`;
                    const initials = `${user.firstName[0]}${user.lastName[0]}`;

                    return (
                      <button
                        key={user.id}
                        onClick={() => handleSelectUser(user)}
                        className="w-full flex items-center gap-3 p-3 hover:bg-muted transition-colors text-left"
                      >
                        <Avatar className="h-9 w-9">
                          <AvatarImage src={user.profilePhotoUrl || undefined} />
                          <AvatarFallback>{initials}</AvatarFallback>
                        </Avatar>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium truncate">{name}</p>
                          <p className="text-sm text-muted-foreground truncate">{user.email}</p>
                        </div>
                        <Check className="h-4 w-4 text-muted-foreground opacity-0 group-hover:opacity-100" />
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={handleClose}>
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              disabled={selectedUsers.length === 0 || createMutation.isPending}
            >
              {createMutation.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              {isGroup ? 'Create Group' : 'Start Chat'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
