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
    // Check if 401 and try to refresh
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
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

export const api = {
  // Auth
  login: (data: any) => apiClient.post('/auth/login', data),
  getMe: () => apiClient.get('/auth/me'),

  // Dashboard
  getDashboardOverview: () => apiClient.get('/dashboard/overview'),

  // Workflow
  getApprovalQueue: (params?: any) => apiClient.get('/approval-queue', { params }),
  approveRequest: (id: string, data?: any) => apiClient.post(`/workflow-instances/${id}/approve`, data),
  rejectRequest: (id: string, data?: any) => apiClient.post(`/workflow-instances/${id}/reject`, data),

  // Organization
  getDepartments: (params?: any) => apiClient.get('/departments', { params }),
  getDepartment: (id: string) => apiClient.get(`/departments/${id}`),
  createDepartment: (data: any) => apiClient.post('/departments', data),
  updateDepartment: (id: string, data: any) => apiClient.put(`/departments/${id}`, data),
  deleteDepartment: (id: string) => apiClient.delete(`/departments/${id}`),
  getDepartmentTree: () => apiClient.get('/departments/tree'),
  getDepartmentEmployees: (id: string, params?: any) => apiClient.get(`/departments/${id}/employees`, { params }),

  getEmployees: (params?: any) => apiClient.get('/employees', { params }),
  getEmployee: (id: string) => apiClient.get(`/employees/${id}`),
  createEmployee: (data: any) => apiClient.post('/employees', data),
  updateEmployee: (id: string, data: any) => apiClient.put(`/employees/${id}`, data),
  deleteEmployee: (id: string) => apiClient.delete(`/employees/${id}`),

  getPositions: (params?: any) => apiClient.get('/positions', { params }),
  getPosition: (id: string) => apiClient.get(`/positions/${id}`),
  createPosition: (data: any) => apiClient.post('/positions', data),
  updatePosition: (id: string, data: any) => apiClient.put(`/positions/${id}`, data),
  deletePosition: (id: string) => apiClient.delete(`/positions/${id}`),

  // Finance
  getBudgets: (params?: any) => apiClient.get('/budgets', { params }),
  getBudget: (id: string) => apiClient.get(`/budgets/${id}`),
  createBudget: (data: any) => apiClient.post('/budgets', data),
  updateBudget: (id: string, data: any) => apiClient.put(`/budgets/${id}`, data),
  deleteBudget: (id: string) => apiClient.delete(`/budgets/${id}`),

  getCategories: (params?: any) => apiClient.get('/budget-categories', { params }),
  getCategory: (id: string) => apiClient.get(`/budget-categories/${id}`),
  createCategory: (data: any) => apiClient.post('/budget-categories', data),
  updateCategory: (id: string, data: any) => apiClient.put(`/budget-categories/${id}`, data),
  deleteCategory: (id: string) => apiClient.delete(`/budget-categories/${id}`),

  getVendors: (params?: any) => apiClient.get('/vendors', { params }),
  getVendor: (id: string) => apiClient.get(`/vendors/${id}`),
  createVendor: (data: any) => apiClient.post('/vendors', data),
  updateVendor: (id: string, data: any) => apiClient.put(`/vendors/${id}`, data),
  deleteVendor: (id: string) => apiClient.delete(`/vendors/${id}`),

  getWallets: (params?: any) => apiClient.get('/wallets', { params }),
  getWallet: (id: string) => apiClient.get(`/wallets/${id}`),
  createWallet: (data: any) => apiClient.post('/wallets', data),
  updateWallet: (id: string, data: any) => apiClient.put(`/wallets/${id}`, data),
  deleteWallet: (id: string) => apiClient.delete(`/wallets/${id}`),

  getPaymentRequests: (params?: any) => apiClient.get('/payment-requests', { params }),
  getPaymentRequest: (id: string) => apiClient.get(`/payment-requests/${id}`),
  createPaymentRequest: (data: any) => apiClient.post('/payment-requests', data),
  updatePaymentRequest: (id: string, data: any) => apiClient.put(`/payment-requests/${id}`, data),
  deletePaymentRequest: (id: string) => apiClient.delete(`/payment-requests/${id}`),

  getTransactions: (params?: any) => apiClient.get('/transactions', { params }),
  getTransaction: (id: string) => apiClient.get(`/transactions/${id}`),
  createTransaction: (data: any) => apiClient.post('/transactions', data),

  // Performance
  getPeriods: (params?: any) => apiClient.get('/evaluation-periods', { params }),
  getPeriod: (id: string) => apiClient.get(`/evaluation-periods/${id}`),
  createPeriod: (data: any) => apiClient.post('/evaluation-periods', data),
  updatePeriod: (id: string, data: any) => apiClient.put(`/evaluation-periods/${id}`, data),

  getObjectives: (params?: any) => apiClient.get('/objectives', { params }),
  getObjective: (id: string) => apiClient.get(`/objectives/${id}`),
  createObjective: (data: any) => apiClient.post('/objectives', data),
  updateObjective: (id: string, data: any) => apiClient.put(`/objectives/${id}`, data),
  deleteObjective: (id: string) => apiClient.delete(`/objectives/${id}`),
  getObjectiveTree: () => apiClient.get('/objectives/tree'),

  getKeyResults: (params?: any) => apiClient.get('/key-results', { params }),
  createKeyResult: (data: any) => apiClient.post('/key-results', data),
  updateKeyResult: (id: string, data: any) => apiClient.put(`/key-results/${id}`, data),
  deleteKeyResult: (id: string) => apiClient.delete(`/key-results/${id}`),

  getKpis: (params?: any) => apiClient.get('/kpis', { params }),
  getKpi: (id: string) => apiClient.get(`/kpis/${id}`),
  createKpi: (data: any) => apiClient.post('/kpis', data),
  updateKpi: (id: string, data: any) => apiClient.put(`/kpis/${id}`, data),
  deleteKpi: (id: string) => apiClient.delete(`/kpis/${id}`),

  getCheckIns: (params?: any) => apiClient.get('/check-ins', { params }),
  createCheckIn: (id: string, data: any) => apiClient.post(`/kpis/${id}/check-in`, data),
  approveCheckIn: (id: string, data: any) => apiClient.post(`/check-ins/${id}/approve`, data),
  rejectCheckIn: (id: string, data: any) => apiClient.post(`/check-ins/${id}/reject`, data),
  getScorecard: (employeeId: string) => apiClient.get(`/kpis/scorecard/${employeeId}`),
};
