import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/stores/auth';

const baseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';

export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = useAuthStore.getState().token;
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

apiClient.interceptors.response.use(
  (response) => {
    // If backend returns { success: false } in 200 response, we could handle it here
    // but typically axios throws on 4xx/5xx
    const data = response.data;
    if (data && typeof data === 'object' && 'success' in data && !data.success) {
      return Promise.reject(new Error(data.message || 'API Error'));
    }
    // Unwrap the generic API response wrapper if applicable
    if (data && typeof data === 'object' && 'data' in data && 'success' in data && data.success) {
      return data.data;
    }
    return response.data;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    
    // Check if 401 and try to refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const refreshToken = useAuthStore.getState().refreshToken;
        if (!refreshToken) throw new Error('No refresh token');

        // Optional: call refresh API (if supported)
        const refreshResponse = await axios.post(`${baseURL}/auth/refresh-token`, { refreshToken });
        const { accessToken, refreshToken: newRefresh } = refreshResponse.data.data;
        
        useAuthStore.getState().updateToken(accessToken, newRefresh);
        
        // Retry original request
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        }
        return apiClient(originalRequest);
      } catch (refreshError) {
        // Refresh failed, clear auth and redirect to login
        useAuthStore.getState().clearAuth();
        if (typeof window !== 'undefined') {
          window.location.href = '/login';
        }
        return Promise.reject(refreshError);
      }
    }

    // Standardize error format { message, errors, traceId }
    if (error.response?.data) {
      const data: any = error.response.data;
      return Promise.reject({
        message: data.message || error.message,
        errors: data.errors || [],
        traceId: data.traceId || '',
      });
    }

    return Promise.reject(error);
  }
);
