import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "./client";
import type { DashboardOverview, Budget, ApprovalQueueItem, PagedResult, PaymentRequestPayload } from "@/types/api";

export const useDashboardOverview = () => {
  return useQuery({
    queryKey: ["dashboard", "overview"],
    queryFn: () => apiClient.get<any, DashboardOverview>("/dashboard/overview"),
  });
};

export const useBudgets = (page = 1, pageSize = 20) => {
  return useQuery({
    queryKey: ["budgets", { page, pageSize }],
    queryFn: () => apiClient.get<any, PagedResult<Budget>>(`/budgets?page=${page}&pageSize=${pageSize}`),
  });
};

export const useApprovalQueue = (page = 1, pageSize = 20) => {
  return useQuery({
    queryKey: ["approval-queue", { page, pageSize }],
    queryFn: () => apiClient.get<any, PagedResult<ApprovalQueueItem>>(`/approval-queue?page=${page}&pageSize=${pageSize}`),
  });
};

export const useCreatePaymentRequest = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: PaymentRequestPayload) => apiClient.post("/payment-requests", payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["approval-queue"] });
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
};
