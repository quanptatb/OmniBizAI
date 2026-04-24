import os

def patch_hooks():
    hooks_path = r"c:\Users\Cua\Desktop\OmniBizAI\frontend\src\lib\api\hooks.ts"
    
    with open(hooks_path, 'a', encoding='utf-8') as f:
        f.write("""
// Organization
export const useDepartments = (page = 1, pageSize = 20) => useQuery({ queryKey: ["departments", { page, pageSize }], queryFn: () => api.getDepartments({ page, pageSize }) });
export const useDepartment = (id: string) => useQuery({ queryKey: ["departments", id], queryFn: () => api.getDepartment(id), enabled: !!id });
export const useEmployees = (page = 1, pageSize = 20) => useQuery({ queryKey: ["employees", { page, pageSize }], queryFn: () => api.getEmployees({ page, pageSize }) });
export const useEmployee = (id: string) => useQuery({ queryKey: ["employees", id], queryFn: () => api.getEmployee(id), enabled: !!id });
export const usePositions = (page = 1, pageSize = 20) => useQuery({ queryKey: ["positions", { page, pageSize }], queryFn: () => api.getPositions({ page, pageSize }) });

// Finance
export const useCategories = (page = 1, pageSize = 20) => useQuery({ queryKey: ["budget-categories", { page, pageSize }], queryFn: () => api.getCategories({ page, pageSize }) });
export const useVendors = (page = 1, pageSize = 20) => useQuery({ queryKey: ["vendors", { page, pageSize }], queryFn: () => api.getVendors({ page, pageSize }) });
export const useWallets = (page = 1, pageSize = 20) => useQuery({ queryKey: ["wallets", { page, pageSize }], queryFn: () => api.getWallets({ page, pageSize }) });
export const usePaymentRequests = (page = 1, pageSize = 20) => useQuery({ queryKey: ["payment-requests", { page, pageSize }], queryFn: () => api.getPaymentRequests({ page, pageSize }) });
export const useTransactions = (page = 1, pageSize = 20) => useQuery({ queryKey: ["transactions", { page, pageSize }], queryFn: () => api.getTransactions({ page, pageSize }) });

// Performance
export const usePeriods = (page = 1, pageSize = 20) => useQuery({ queryKey: ["evaluation-periods", { page, pageSize }], queryFn: () => api.getPeriods({ page, pageSize }) });
export const useObjectives = (page = 1, pageSize = 20) => useQuery({ queryKey: ["objectives", { page, pageSize }], queryFn: () => api.getObjectives({ page, pageSize }) });
export const useKeyResults = (objectiveId?: string, page = 1, pageSize = 20) => useQuery({ queryKey: ["key-results", { objectiveId, page, pageSize }], queryFn: () => api.getKeyResults({ objectiveId, page, pageSize }) });
export const useKpis = (page = 1, pageSize = 20) => useQuery({ queryKey: ["kpis", { page, pageSize }], queryFn: () => api.getKpis({ page, pageSize }) });
export const useCheckIns = (kpiId?: string, page = 1, pageSize = 20) => useQuery({ queryKey: ["check-ins", { kpiId, page, pageSize }], queryFn: () => api.getCheckIns({ kpiId, page, pageSize }) });
""")

if __name__ == "__main__":
    patch_hooks()
