'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FolderOpen } from 'lucide-react';

export default function SpacesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Spaces</h1>
        <p className="text-muted-foreground">
          Browse and manage team workspaces
        </p>
      </div>

      <Card>
        <CardContent className="flex flex-col items-center justify-center py-16">
          <FolderOpen className="h-16 w-16 text-muted-foreground mb-4" />
          <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
            Spaces coming soon
          </h3>
          <p className="text-muted-foreground mt-1">
            This feature is under development
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
