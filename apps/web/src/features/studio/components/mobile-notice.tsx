'use client';

import { Monitor } from 'lucide-react';

export function MobileNotice() {
  return (
    <div className="lg:hidden fixed inset-0 z-[100] flex items-center justify-center bg-slate-50 dark:bg-slate-900 p-6">
      <div className="text-center max-w-md">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-slate-100 dark:bg-slate-800 mb-6">
          <Monitor className="h-8 w-8 text-slate-600 dark:text-slate-400" />
        </div>
        <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100 mb-3">
          Desktop Recommended
        </h2>
        <p className="text-slate-600 dark:text-slate-400">
          The Content Studio is optimized for desktop screens (1280px or wider).
          Please use a larger screen for the best editing experience.
        </p>
      </div>
    </div>
  );
}
