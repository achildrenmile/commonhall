'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore, type User } from '@/lib/auth-store';

type UserRole = User['role'];

interface ProtectedRouteProps {
  children: React.ReactNode;
  minRole?: UserRole;
}

const roleHierarchy: Record<UserRole, number> = {
  User: 0,
  Editor: 1,
  Admin: 2,
};

export function ProtectedRoute({ children, minRole }: ProtectedRouteProps) {
  const router = useRouter();
  const { isAuthenticated, isLoading, user } = useAuthStore();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/login');
    }
  }, [isAuthenticated, isLoading, router]);

  // Check role if minRole is specified
  if (minRole && user) {
    const userRoleLevel = roleHierarchy[user.role];
    const requiredRoleLevel = roleHierarchy[minRole];

    if (userRoleLevel < requiredRoleLevel) {
      return (
        <div className="flex h-screen items-center justify-center">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">
              Access Denied
            </h1>
            <p className="mt-2 text-slate-600 dark:text-slate-400">
              You don&apos;t have permission to access this page.
            </p>
          </div>
        </div>
      );
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return <>{children}</>;
}
