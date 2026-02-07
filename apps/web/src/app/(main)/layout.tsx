import { ProtectedRoute } from '@/components/protected-route';
import { Sidebar } from '@/components/layout/sidebar';
import { Header } from '@/components/layout/header';
import { AnalyticsProvider } from '@/components/providers/analytics-provider';

export default function MainLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute>
      <AnalyticsProvider>
        <div className="flex h-screen overflow-hidden bg-slate-50 dark:bg-slate-900">
          <Sidebar />
          <div className="flex flex-1 flex-col overflow-hidden">
            <Header />
            <main className="flex-1 overflow-y-auto p-4 lg:p-6">
              {children}
            </main>
          </div>
        </div>
      </AnalyticsProvider>
    </ProtectedRoute>
  );
}
