'use client';

import { useEffect, type ReactNode } from 'react';
import { useAuthStore } from '@/lib/auth-store';

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const loadUser = useAuthStore((state) => state.loadUser);
  const isLoading = useAuthStore((state) => state.isLoading);

  useEffect(() => {
    loadUser();
  }, [loadUser]);

  // Show nothing while checking auth status on initial load
  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  return <>{children}</>;
}
