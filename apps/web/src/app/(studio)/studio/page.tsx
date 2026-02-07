'use client';

import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Newspaper, FileText, ArrowLeft, Plus } from 'lucide-react';

export default function StudioPage() {
  return (
    <div className="container mx-auto py-8 px-4 max-w-4xl">
      {/* Header */}
      <div className="mb-8">
        <Link href="/">
          <Button variant="ghost" size="sm" className="mb-4">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Button>
        </Link>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100">
          Content Studio
        </h1>
        <p className="text-muted-foreground mt-2">
          Create and manage content for your organization
        </p>
      </div>

      {/* Create options */}
      <div className="grid gap-6 md:grid-cols-2">
        <Card className="hover:shadow-md transition-shadow cursor-pointer group">
          <CardHeader>
            <div className="flex items-center gap-4">
              <div className="p-3 rounded-lg bg-blue-500/10 text-blue-600 dark:text-blue-400">
                <Newspaper className="h-6 w-6" />
              </div>
              <div>
                <CardTitle className="flex items-center gap-2">
                  News Article
                  <Plus className="h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" />
                </CardTitle>
                <CardDescription>
                  Write and publish news for your organization
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Create announcements, updates, and stories to keep your team informed.
            </p>
          </CardContent>
        </Card>

        <Card className="hover:shadow-md transition-shadow cursor-pointer group">
          <CardHeader>
            <div className="flex items-center gap-4">
              <div className="p-3 rounded-lg bg-purple-500/10 text-purple-600 dark:text-purple-400">
                <FileText className="h-6 w-6" />
              </div>
              <div>
                <CardTitle className="flex items-center gap-2">
                  Knowledge Page
                  <Plus className="h-4 w-4 opacity-0 group-hover:opacity-100 transition-opacity" />
                </CardTitle>
                <CardDescription>
                  Create documentation and knowledge base articles
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Build a comprehensive knowledge base for your organization.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
