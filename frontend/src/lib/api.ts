import type { ApiResponse, DashboardOverview, LoginResponse, PaymentRequestPayload } from "@/types/api";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api/v1";

export async function apiRequest<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers
    }
  });
  const body = (await response.json()) as ApiResponse<T>;
  if (!response.ok || !body.success) {
    throw new Error(body.message || "Request failed");
  }
  return body.data;
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
}

export function getDashboard(token: string): Promise<DashboardOverview> {
  return apiRequest<DashboardOverview>("/dashboard/overview", {}, token);
}

export function createPaymentRequest(token: string, payload: PaymentRequestPayload) {
  return apiRequest("/payment-requests", {
    method: "POST",
    body: JSON.stringify(payload)
  }, token);
}

export function askAi(token: string, message: string) {
  return apiRequest<{ sessionId: string; content: string; citations: unknown[] }>("/ai/chat", {
    method: "POST",
    body: JSON.stringify({ message, contextType: "Dashboard" })
  }, token);
}
