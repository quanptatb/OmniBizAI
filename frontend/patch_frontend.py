import os

def patch_api_client():
    client_path = r"c:\Users\Cua\Desktop\OmniBizAI\frontend\src\lib\api\client.ts"
    
    with open(client_path, 'r', encoding='utf-8') as f:
        content = f.read()

    org_patch = """
  // Organization
  getDepartments: (params?: any) => api.get('/departments', { params }),
  getDepartment: (id: string) => api.get(`/departments/${id}`),
  updateDepartment: (id: string, data: any) => api.put(`/departments/${id}`, data),
  deleteDepartment: (id: string) => api.delete(`/departments/${id}`),
  getDepartmentTree: () => api.get('/departments/tree'),
  
  getEmployees: (params?: any) => api.get('/employees', { params }),
  getEmployee: (id: string) => api.get(`/employees/${id}`),
  updateEmployee: (id: string, data: any) => api.put(`/employees/${id}`, data),
  deleteEmployee: (id: string) => api.delete(`/employees/${id}`),
  
  getPositions: (params?: any) => api.get('/positions', { params }),
  getPosition: (id: string) => api.get(`/positions/${id}`),
  createPosition: (data: any) => api.post('/positions', data),
  updatePosition: (id: string, data: any) => api.put(`/positions/${id}`, data),
  deletePosition: (id: string) => api.delete(`/positions/${id}`),
"""
    fin_patch = """
  // Finance
  getBudget: (id: string) => api.get(`/budgets/${id}`),
  updateBudget: (id: string, data: any) => api.put(`/budgets/${id}`, data),
  deleteBudget: (id: string) => api.delete(`/budgets/${id}`),
  
  getCategories: (params?: any) => api.get('/budget-categories', { params }),
  
  getVendors: (params?: any) => api.get('/vendors', { params }),
  getVendor: (id: string) => api.get(`/vendors/${id}`),
  createVendor: (data: any) => api.post('/vendors', data),
  updateVendor: (id: string, data: any) => api.put(`/vendors/${id}`, data),
  
  getWallets: (params?: any) => api.get('/wallets', { params }),
  getWallet: (id: string) => api.get(`/wallets/${id}`),
  createWallet: (data: any) => api.post('/wallets', data),
  updateWallet: (id: string, data: any) => api.put(`/wallets/${id}`, data),
  
  getPaymentRequests: (params?: any) => api.get('/payment-requests', { params }),
  getPaymentRequest: (id: string) => api.get(`/payment-requests/${id}`),
  updatePaymentRequest: (id: string, data: any) => api.put(`/payment-requests/${id}`, data),
  deletePaymentRequest: (id: string) => api.delete(`/payment-requests/${id}`),
  
  getTransactions: (params?: any) => api.get('/transactions', { params }),
  getTransaction: (id: string) => api.get(`/transactions/${id}`),
"""
    perf_patch = """
  // Performance
  getPeriods: (params?: any) => api.get('/evaluation-periods', { params }),
  
  getObjectives: (params?: any) => api.get('/objectives', { params }),
  getObjective: (id: string) => api.get(`/objectives/${id}`),
  updateObjective: (id: string, data: any) => api.put(`/objectives/${id}`, data),
  deleteObjective: (id: string) => api.delete(`/objectives/${id}`),
  getObjectiveTree: () => api.get('/objectives/tree'),
  
  getKeyResults: (params?: any) => api.get('/key-results', { params }),
  updateKeyResult: (id: string, data: any) => api.put(`/key-results/${id}`, data),
  deleteKeyResult: (id: string) => api.delete(`/key-results/${id}`),
  
  getKpis: (params?: any) => api.get('/kpis', { params }),
  getKpi: (id: string) => api.get(`/kpis/${id}`),
  updateKpi: (id: string, data: any) => api.put(`/kpis/${id}`, data),
  deleteKpi: (id: string) => api.delete(`/kpis/${id}`),
  
  getCheckIns: (params?: any) => api.get('/check-ins', { params }),
  getScorecard: (employeeId: string) => api.get(`/scorecard/${employeeId}`),
"""
    
    # We will just append these into the exported `apiClient` object before its closing brace.
    if "// Organization" not in content:
        last_brace = content.rfind('};')
        if last_brace != -1:
            new_content = content[:last_brace] + org_patch + fin_patch + perf_patch + content[last_brace:]
            with open(client_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            print("Patched client.ts")

if __name__ == "__main__":
    patch_api_client()
