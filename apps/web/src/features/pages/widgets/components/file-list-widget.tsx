'use client';

import { useQuery } from '@tanstack/react-query';
import { FileText, Image, FileVideo, FileAudio, File, Download, Loader2 } from 'lucide-react';
import { apiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { formatBytes } from '@/lib/utils';
import type { WidgetProps, FileListData } from '../types';

interface FileInfo {
  id: string;
  fileName: string;
  originalFileName: string;
  mimeType: string;
  size: number;
  downloadUrl: string;
}

function getFileIcon(mimeType: string) {
  if (mimeType.startsWith('image/')) return Image;
  if (mimeType.startsWith('video/')) return FileVideo;
  if (mimeType.startsWith('audio/')) return FileAudio;
  if (mimeType.includes('pdf') || mimeType.includes('document') || mimeType.includes('text')) return FileText;
  return File;
}

async function fetchFiles(fileIds: string[]): Promise<FileInfo[]> {
  if (fileIds.length === 0) return [];

  const results = await Promise.all(
    fileIds.map(async (id) => {
      try {
        const response = await apiClient.get<FileInfo>(`/files/${id}`);
        return response;
      } catch {
        return null;
      }
    })
  );

  return results.filter((f): f is FileInfo => f !== null);
}

export default function FileListWidget({ data, id }: WidgetProps<FileListData>) {
  const { data: files, isLoading, error } = useQuery({
    queryKey: ['widget-files', id, data.fileIds],
    queryFn: () => fetchFiles(data.fileIds || []),
    enabled: (data.fileIds?.length ?? 0) > 0,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (error || !files || files.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 dark:border-slate-800 p-4 text-center text-sm text-slate-500">
        No files available
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-800 overflow-hidden">
      {data.title && (
        <div className="bg-slate-50 dark:bg-slate-900 px-4 py-3 border-b border-slate-200 dark:border-slate-800">
          <h3 className="font-medium text-slate-900 dark:text-slate-100">
            {data.title}
          </h3>
        </div>
      )}
      <ul className="divide-y divide-slate-200 dark:divide-slate-800">
        {files.map((file) => {
          const Icon = getFileIcon(file.mimeType);
          return (
            <li key={file.id} className="flex items-center gap-3 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-900">
              <Icon className="h-5 w-5 text-slate-400 shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-slate-900 dark:text-slate-100 truncate">
                  {file.originalFileName}
                </p>
                <p className="text-xs text-slate-500">
                  {formatBytes(file.size)}
                </p>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <a href={file.downloadUrl} download={file.originalFileName}>
                  <Download className="h-4 w-4" />
                </a>
              </Button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
