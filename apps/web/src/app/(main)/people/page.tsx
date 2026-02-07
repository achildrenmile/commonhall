'use client';

import { Card, CardContent } from '@/components/ui/card';
import { Users } from 'lucide-react';

export default function PeoplePage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">People</h1>
        <p className="text-muted-foreground">
          Find colleagues and browse the organization directory
        </p>
      </div>

      <Card>
        <CardContent className="flex flex-col items-center justify-center py-16">
          <Users className="h-16 w-16 text-muted-foreground mb-4" />
          <h3 className="text-lg font-medium text-slate-900 dark:text-slate-100">
            People directory coming soon
          </h3>
          <p className="text-muted-foreground mt-1">
            This feature is under development
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
