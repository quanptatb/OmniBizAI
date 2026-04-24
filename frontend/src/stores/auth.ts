import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { LoginResponse } from '@/types/api';

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: LoginResponse['user'] | null;
  setAuth: (data: LoginResponse) => void;
  clearAuth: () => void;
  updateToken: (accessToken: string, refreshToken: string) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      user: null,
      setAuth: (data) => set({
        token: data.accessToken,
        refreshToken: data.refreshToken,
        user: data.user,
      }),
      clearAuth: () => set({ token: null, refreshToken: null, user: null }),
      updateToken: (accessToken, refreshToken) => set({ token: accessToken, refreshToken }),
    }),
    {
      name: 'omnibiz-auth-store', // name of the item in the storage (must be unique)
    }
  )
);
