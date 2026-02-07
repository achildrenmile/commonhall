import { ProtectedRoute } from '@/components/protected-route';
import { StudioHeader, StudioSidebar, MobileNotice } from '@/features/studio';

export default function StudioLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute minRole="Editor">
      <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
        {/* Mobile Notice */}
        <MobileNotice />

        {/* Header */}
        <StudioHeader />

        {/* Sidebar */}
        <StudioSidebar />

        {/* Main Content */}
        <main className="lg:pl-56 pt-14 min-h-screen">
          <div className="p-6">
            {children}
          </div>
        </main>
      </div>
    </ProtectedRoute>
  );
}
