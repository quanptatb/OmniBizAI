import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "./client";
import type {
  ApprovalQueueItem,
  Budget,
  BudgetCategory,
  DashboardOverview,
  Department,
  Employee,
  EvaluationPeriod,
  KeyResult,
  Kpi,
  KpiCheckIn,
  Notification,
  Objective,
  PagedResult,
  PaymentRequest,
  PaymentRequestPayload,
  Position,
  Transaction,
  Vendor,
  Wallet,
} from "@/types/api";

const pageParams = (page = 1, pageSize = 20, search?: string) => ({ page, pageSize, search });

export const useDashboardOverview = () =>
  useQuery({
    queryKey: ["dashboard", "overview"],
    queryFn: () => apiClient.get<any, DashboardOverview>("/dashboard/overview"),
  });

export const useApprovalQueue = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["approval-queue", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<ApprovalQueueItem>>("/approval-queue", { params: pageParams(page, pageSize, search) }),
  });

export const useDepartments = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["departments", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Department>>("/departments", { params: pageParams(page, pageSize, search) }),
  });

export const useEmployees = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["employees", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Employee>>("/employees", { params: pageParams(page, pageSize, search) }),
  });

export const usePositions = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["positions", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Position>>("/positions", { params: pageParams(page, pageSize, search) }),
  });

export const useBudgets = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["budgets", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Budget>>("/budgets", { params: pageParams(page, pageSize, search) }),
  });

export const useCategories = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["budget-categories", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<BudgetCategory>>("/budget-categories", { params: pageParams(page, pageSize, search) }),
  });

export const useVendors = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["vendors", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Vendor>>("/vendors", { params: pageParams(page, pageSize, search) }),
  });

export const useWallets = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["wallets", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Wallet>>("/wallets", { params: pageParams(page, pageSize, search) }),
  });

export const usePaymentRequests = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["payment-requests", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<PaymentRequest>>("/payment-requests", { params: pageParams(page, pageSize, search) }),
  });

export const useTransactions = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["transactions", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Transaction>>("/transactions", { params: pageParams(page, pageSize, search) }),
  });

export const usePeriods = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["evaluation-periods", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<EvaluationPeriod>>("/evaluation-periods", { params: pageParams(page, pageSize, search) }),
  });

export const useObjectives = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["objectives", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Objective>>("/objectives", { params: pageParams(page, pageSize, search) }),
  });

export const useKeyResults = (objectiveId?: string, page = 1, pageSize = 100) =>
  useQuery({
    queryKey: ["key-results", { objectiveId, page, pageSize }],
    queryFn: () => apiClient.get<any, PagedResult<KeyResult>>("/key-results", { params: { objectiveId, page, pageSize } }),
  });

export const useKpis = (page = 1, pageSize = 20, search?: string) =>
  useQuery({
    queryKey: ["kpis", { page, pageSize, search }],
    queryFn: () => apiClient.get<any, PagedResult<Kpi>>("/kpis", { params: pageParams(page, pageSize, search) }),
  });

export const useCheckIns = (kpiId?: string, page = 1, pageSize = 20, status?: string) =>
  useQuery({
    queryKey: ["check-ins", { kpiId, page, pageSize, status }],
    queryFn: () => apiClient.get<any, PagedResult<KpiCheckIn>>("/check-ins", { params: { kpiId, status, page, pageSize } }),
  });

export const useNotifications = (page = 1, pageSize = 20) =>
  useQuery({
    queryKey: ["notifications", { page, pageSize }],
    queryFn: () => apiClient.get<any, PagedResult<Notification>>("/notifications", { params: { page, pageSize } }),
  });

export const useUnreadNotifications = () =>
  useQuery({
    queryKey: ["notifications", "unread-count"],
    queryFn: () => apiClient.get<any, { count: number }>("/notifications/unread-count"),
  });

export const useCreatePaymentRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: PaymentRequestPayload) => apiClient.post<any, PaymentRequest>("/payment-requests", payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payment-requests"] });
      queryClient.invalidateQueries({ queryKey: ["approval-queue"] });
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
};

export const useSubmitPaymentRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.post<any, PaymentRequest>(`/payment-requests/${id}/submit`, {}),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["payment-requests"] });
      queryClient.invalidateQueries({ queryKey: ["approval-queue"] });
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
};

export const useWorkflowDecision = (action: "approve" | "reject") => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      apiClient.post(`/workflow-instances/${id}/${action}`, { comment }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["approval-queue"] });
      queryClient.invalidateQueries({ queryKey: ["payment-requests"] });
      queryClient.invalidateQueries({ queryKey: ["notifications"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
};

export const useCheckInDecision = (action: "approve" | "reject") => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      apiClient.post(`/check-ins/${id}/${action}`, { comment }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["check-ins"] });
      queryClient.invalidateQueries({ queryKey: ["kpis"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
};
