'use client';

import { useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { ChevronLeft, Download, Loader2, Inbox, User, Calendar } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { useForm, useFormSubmissions, exportFormCsv } from '../api';
import type { FormField, FormSubmission } from '../types';
import { formatRelativeTime } from '@/lib/utils';

interface FormSubmissionsProps {
  id: string;
}

function parseFields(fieldsJson: string): FormField[] {
  try {
    return JSON.parse(fieldsJson);
  } catch {
    return [];
  }
}

function parseSubmissionData(dataJson: string): Record<string, unknown> {
  try {
    return JSON.parse(dataJson);
  } catch {
    return {};
  }
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return '-';
  if (Array.isArray(value)) return value.join(', ');
  if (typeof value === 'object') return JSON.stringify(value);
  return String(value);
}

export function FormSubmissions({ id }: FormSubmissionsProps) {
  const router = useRouter();
  const { data: form, isLoading: formLoading } = useForm(id);
  const {
    data: submissionsData,
    isLoading: submissionsLoading,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useFormSubmissions(id);

  const isLoading = formLoading || submissionsLoading;

  const fields = useMemo(() => {
    if (!form) return [];
    return parseFields(form.fields);
  }, [form]);

  const submissions = useMemo(() => {
    if (!submissionsData) return [];
    return submissionsData.pages.flatMap((page) => page.items);
  }, [submissionsData]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!form) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Form not found</p>
      </div>
    );
  }

  const handleExport = () => {
    window.open(exportFormCsv(id), '_blank');
  };

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="border-b bg-background px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button variant="ghost" size="icon" onClick={() => router.push(`/studio/forms/${id}`)}>
              <ChevronLeft className="h-5 w-5" />
            </Button>
            <div>
              <h1 className="text-lg font-semibold">{form.title}</h1>
              <p className="text-sm text-muted-foreground">Submissions</p>
            </div>
            <Badge variant={form.isActive ? 'default' : 'secondary'}>
              {form.isActive ? 'Active' : 'Inactive'}
            </Badge>
          </div>
          <Button variant="outline" onClick={handleExport}>
            <Download className="h-4 w-4 mr-2" />
            Export CSV
          </Button>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-6">
        {submissions.length === 0 ? (
          <div className="text-center py-16">
            <Inbox className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <h3 className="font-semibold mb-2">No submissions yet</h3>
            <p className="text-sm text-muted-foreground">
              Submissions will appear here once users start filling out the form
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="rounded-lg border overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[200px]">Submitted</TableHead>
                    <TableHead className="w-[200px]">User</TableHead>
                    {fields.slice(0, 4).map((field) => (
                      <TableHead key={field.name}>
                        {field.label || field.name}
                      </TableHead>
                    ))}
                    {fields.length > 4 && (
                      <TableHead className="w-[100px]">...</TableHead>
                    )}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {submissions.map((submission) => {
                    const data = parseSubmissionData(submission.data);
                    return (
                      <TableRow key={submission.id}>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm">
                            <Calendar className="h-4 w-4 text-muted-foreground" />
                            {formatRelativeTime(submission.createdAt)}
                          </div>
                        </TableCell>
                        <TableCell>
                          {submission.userName || submission.userEmail ? (
                            <div className="flex items-center gap-2">
                              <User className="h-4 w-4 text-muted-foreground" />
                              <div>
                                {submission.userName && (
                                  <div className="font-medium">{submission.userName}</div>
                                )}
                                {submission.userEmail && (
                                  <div className="text-xs text-muted-foreground">
                                    {submission.userEmail}
                                  </div>
                                )}
                              </div>
                            </div>
                          ) : (
                            <span className="text-muted-foreground">Anonymous</span>
                          )}
                        </TableCell>
                        {fields.slice(0, 4).map((field) => (
                          <TableCell key={field.name} className="max-w-[200px] truncate">
                            {formatValue(data[field.name])}
                          </TableCell>
                        ))}
                        {fields.length > 4 && (
                          <TableCell className="text-muted-foreground">
                            +{fields.length - 4} more
                          </TableCell>
                        )}
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            {hasNextPage && (
              <div className="text-center">
                <Button
                  variant="outline"
                  onClick={() => fetchNextPage()}
                  disabled={isFetchingNextPage}
                >
                  {isFetchingNextPage ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Loading...
                    </>
                  ) : (
                    'Load More'
                  )}
                </Button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
