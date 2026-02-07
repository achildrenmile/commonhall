import { ProtectedRoute } from '@/components/protected-route';

export default function StudioLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute minRole="Editor">
      <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
        {children}
      </div>
    </ProtectedRoute>
  );
}
