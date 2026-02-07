'use client';

import { useState } from 'react';
import Link from 'next/link';
import {
  Plus,
  FileText,
  MoreHorizontal,
  Trash2,
  FileDown,
  Eye,
  EyeOff,
  Loader2,
  Inbox,
  Calendar,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useToast } from '@/hooks/use-toast';
import { formatRelativeTime } from '@/lib/utils';
import { useForms, useDeleteForm, useUpdateForm, exportFormCsv } from '../api';
import type { FormListItem } from '../types';

export function FormList() {
  const { toast } = useToast();
  const [activeFilter, setActiveFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: forms, isLoading } = useForms(
    activeFilter === 'all' ? undefined : activeFilter === 'active'
  );
  const deleteMutation = useDeleteForm();
  const updateMutation = useUpdateForm();

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      await deleteMutation.mutateAsync(deleteId);
      toast({ title: 'Form deleted' });
      setDeleteId(null);
    } catch {
      toast({ title: 'Error', description: 'Failed to delete form', variant: 'destructive' });
    }
  };

  const handleToggleActive = async (form: FormListItem) => {
    try {
      await updateMutation.mutateAsync({
        id: form.id,
        isActive: !form.isActive,
      });
      toast({ title: form.isActive ? 'Form deactivated' : 'Form activated' });
    } catch {
      toast({ title: 'Error', description: 'Failed to update form', variant: 'destructive' });
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Forms</h1>
          <p className="text-sm text-muted-foreground">
            Create and manage contact forms and submissions
          </p>
        </div>
        <Button asChild>
          <Link href="/studio/forms/new">
            <Plus className="h-4 w-4 mr-2" />
            Create Form
          </Link>
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <Select value={activeFilter} onValueChange={(v) => setActiveFilter(v as 'all' | 'active' | 'inactive')}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Forms</SelectItem>
            <SelectItem value="active">Active</SelectItem>
            <SelectItem value="inactive">Inactive</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {!forms?.length ? (
        <div className="text-center py-16 bg-muted/30 rounded-lg border border-dashed">
          <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="font-semibold mb-2">No forms yet</h3>
          <p className="text-sm text-muted-foreground mb-4">
            Create your first form to collect submissions
          </p>
          <Button asChild>
            <Link href="/studio/forms/new">
              <Plus className="h-4 w-4 mr-2" />
              Create Form
            </Link>
          </Button>
        </div>
      ) : (
        <div className="grid gap-4">
          {forms.map((form) => (
            <div
              key={form.id}
              className="border rounded-lg p-4 hover:border-foreground/20 transition-colors bg-card"
            >
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <Link
                      href={`/studio/forms/${form.id}`}
                      className="font-semibold hover:underline truncate"
                    >
                      {form.title}
                    </Link>
                    <Badge variant={form.isActive ? 'default' : 'secondary'}>
                      {form.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </div>
                  {form.description && (
                    <p className="text-sm text-muted-foreground line-clamp-1 mb-2">
                      {form.description}
                    </p>
                  )}
                  <div className="flex items-center gap-4 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Inbox className="h-3 w-3" />
                      {form.submissionCount} submissions
                    </span>
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      Created {formatRelativeTime(form.createdAt)}
                    </span>
                  </div>
                </div>

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="h-8 w-8">
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem asChild>
                      <Link href={`/studio/forms/${form.id}`}>
                        Edit
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <Link href={`/studio/forms/${form.id}/submissions`}>
                        <Inbox className="h-4 w-4 mr-2" />
                        Submissions
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem asChild>
                      <a href={exportFormCsv(form.id)} target="_blank" rel="noopener noreferrer">
                        <FileDown className="h-4 w-4 mr-2" />
                        Export CSV
                      </a>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleToggleActive(form)}>
                      {form.isActive ? (
                        <>
                          <EyeOff className="h-4 w-4 mr-2" />
                          Deactivate
                        </>
                      ) : (
                        <>
                          <Eye className="h-4 w-4 mr-2" />
                          Activate
                        </>
                      )}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      className="text-destructive"
                      onClick={() => setDeleteId(form.id)}
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
          ))}
        </div>
      )}

      <AlertDialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Form</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete this form and all its submissions. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
