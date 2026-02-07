import { create } from 'zustand';
import { apiClient, setTokens, clearTokens } from './api-client';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  avatarUrl?: string;
  role: 'User' | 'Editor' | 'Admin';
}

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

interface AuthActions {
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName: string, lastName: string) => Promise<void>;
  logout: () => void;
  refreshTokens: () => Promise<void>;
  loadUser: () => Promise<void>;
  setUser: (user: User | null) => void;
}

type AuthStore = AuthState & AuthActions;

export const useAuthStore = create<AuthStore>((set, get) => ({
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: true,

  login: async (email: string, password: string) => {
    set({ isLoading: true });
    try {
      const response = await apiClient.post('/api/v1/auth/login', { email, password });
      const { accessToken, refreshToken, user } = response.data;

      setTokens(accessToken, refreshToken);

      set({
        user,
        accessToken,
        refreshToken,
        isAuthenticated: true,
        isLoading: false,
      });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  register: async (email: string, password: string, firstName: string, lastName: string) => {
    set({ isLoading: true });
    try {
      await apiClient.post('/api/v1/auth/register', {
        email,
        password,
        firstName,
        lastName,
      });
      set({ isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  logout: () => {
    clearTokens();
    set({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,
    });
  },

  refreshTokens: async () => {
    const { refreshToken } = get();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    try {
      const response = await apiClient.post('/api/v1/auth/refresh', { refreshToken });
      const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data;

      setTokens(newAccessToken, newRefreshToken);

      set({
        accessToken: newAccessToken,
        refreshToken: newRefreshToken,
      });
    } catch (error) {
      get().logout();
      throw error;
    }
  },

  loadUser: async () => {
    set({ isLoading: true });
    try {
      const response = await apiClient.get('/api/v1/auth/me');
      set({
        user: response.data,
        isAuthenticated: true,
        isLoading: false,
      });
    } catch {
      set({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  },

  setUser: (user: User | null) => {
    set({ user, isAuthenticated: !!user });
  },
}));
