export type ApiResponse<T> = {
  success: boolean;
  data: T;
  message: string;
  errors?: Array<{ field: string; message: string }>;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: {
    id: string;
    email: string;
    fullName: string;
    roles: string[];
    permissions: string[];
    departmentId?: string;
  };
};

export type DashboardOverview = {
  totalIncome: number;
  totalExpense: number;
  remainingBudget: number;
  averageKpiProgress: number;
  pendingApprovals: number;
  riskAlerts: string[];
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
};

export type Budget = {
  id: string;
  name: string;
  departmentId: string;
  categoryId: string;
  fiscalPeriodId: string;
  allocatedAmount: number;
  spentAmount: number;
  committedAmount: number;
  remainingAmount: number;
  utilizationPercent: number;
  warningLevel: string;
  status: string;
};

export type ApprovalQueueItem = {
  instanceId: string;
  entityType: string;
  entityId: string;
  currentStepOrder: number;
  status: string;
  initiatedAt: string;
};

export type PaymentRequestItem = {
  description: string;
  quantity: number;
  unit?: string;
  unitPrice: number;
  totalPrice: number;
};

export type PaymentRequestPayload = {
  title: string;
  description?: string;
  departmentId: string;
  requesterId: string;
  vendorId?: string;
  budgetId?: string;
  categoryId: string;
  currency: string;
  paymentMethod?: string;
  paymentDueDate?: string;
  priority: string;
  items: PaymentRequestItem[];
};
