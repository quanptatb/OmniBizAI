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

export type Department = {
  id: string;
  name: string;
  code: string;
  parentDepartmentId?: string;
  managerId?: string;
  budgetLimit: number;
  isActive: boolean;
};

export type Employee = {
  id: string;
  userId?: string;
  employeeCode: string;
  fullName: string;
  email: string;
  departmentId?: string;
  positionId?: string;
  managerId?: string;
  status: string;
};

export type Position = {
  id: string;
  name: string;
  level: number;
  departmentId?: string;
  description?: string;
  isActive: boolean;
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

export type BudgetCategory = {
  id: string;
  name: string;
  code: string;
  type: "Income" | "Expense";
  parentId?: string;
  isActive: boolean;
};

export type Vendor = {
  id: string;
  name: string;
  taxCode?: string;
  email?: string;
  phone?: string;
  rating?: number;
  status: string;
};

export type Wallet = {
  id: string;
  name: string;
  type: string;
  balance: number;
  currency: string;
  isActive: boolean;
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
  id?: string;
  description: string;
  quantity: number;
  unit?: string;
  unitPrice: number;
  totalPrice: number;
};

export type PaymentRequest = {
  id: string;
  requestNumber: string;
  title: string;
  departmentId: string;
  requesterId: string;
  vendorId?: string;
  budgetId?: string;
  categoryId: string;
  totalAmount: number;
  currency: string;
  status: string;
  aiRiskScore?: number;
  items: PaymentRequestItem[];
  description?: string;
  priority?: string;
  paymentDueDate?: string;
  submittedAt?: string;
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

export type Transaction = {
  id: string;
  transactionNumber: string;
  type: "Income" | "Expense";
  amount: number;
  walletId: string;
  departmentId: string;
  categoryId: string;
  budgetId?: string;
  transactionDate: string;
  status: string;
};

export type EvaluationPeriod = {
  id: string;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  status: string;
};

export type Objective = {
  id: string;
  title: string;
  periodId: string;
  ownerType: "Company" | "Department" | "Individual";
  departmentId?: string;
  ownerId?: string;
  progress: number;
  status: string;
};

export type KeyResult = {
  id: string;
  objectiveId: string;
  title: string;
  metricType: string;
  startValue: number;
  targetValue: number;
  currentValue: number;
  progress: number;
  weight: number;
};

export type Kpi = {
  id: string;
  name: string;
  periodId: string;
  departmentId?: string;
  assigneeId?: string;
  metricType: string;
  targetValue: number;
  currentValue: number;
  progress: number;
  weight: number;
  rating?: string;
  status: string;
};

export type KpiCheckIn = {
  id: string;
  kpiId: string;
  checkInDate: string;
  previousValue?: number;
  newValue: number;
  progress?: number;
  note: string;
  status: string;
  reviewComment?: string;
};

export type Notification = {
  id: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  entityType?: string;
  entityId?: string;
  actionUrl?: string;
  isRead: boolean;
  createdAt: string;
};
