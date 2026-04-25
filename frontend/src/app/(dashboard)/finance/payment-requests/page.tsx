"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { AlertTriangle, Filter, Plus, Search, Send } from "lucide-react";
import { PageHeader } from "@/components/ui/PageHeader";
import { usePaymentRequests, useSubmitPaymentRequest } from "@/lib/api/hooks";
import { cn } from "@/lib/utils";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });

const tabs = [
  { id: "all", label: "Tất cả" },
  { id: "draft", label: "Nháp" },
  { id: "pendingapproval", label: "Chờ duyệt" },
  { id: "approved", label: "Đã duyệt" },
  { id: "rejected", label: "Từ chối" },
];

export default function PaymentRequestsPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [search, setSearch] = useState("");
  const { data, isLoading } = usePaymentRequests(1, 50, search);
  const submitMutation = useSubmitPaymentRequest();

  const requests = useMemo(() => {
    const items = data?.items ?? [];
    if (activeTab === "all") return items;
    return items.filter((item) => item.status.toLowerCase() === activeTab);
  }, [activeTab, data?.items]);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Đề nghị thanh toán"
        description="Theo dõi đề nghị chi, cảnh báo AI và trạng thái phê duyệt."
      >
        <Link href="/finance/payment-requests/new" className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary-dark transition-colors font-semibold text-sm">
          <Plus size={16} />
          Tạo đề nghị
        </Link>
      </PageHeader>

      <div className="bg-white rounded-lg border border-border shadow-sm overflow-hidden">
        <div className="border-b border-border overflow-x-auto">
          <nav className="flex gap-6 px-6 min-w-max" aria-label="Tabs">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={cn(
                  "py-4 px-1 inline-flex items-center border-b-2 font-semibold text-sm transition-colors",
                  activeTab === tab.id ? "border-primary text-primary" : "border-transparent text-text-muted hover:text-text-primary"
                )}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        <div className="p-4 border-b border-border flex flex-col sm:flex-row gap-4 justify-between items-center bg-[#f8fafe]">
          <div className="relative w-full sm:w-80">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" />
            <input
              type="text"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm theo mã hoặc tiêu đề"
              className="w-full h-10 pl-9 pr-4 rounded-md border border-border text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
            />
          </div>
          <button className="inline-flex items-center gap-2 px-3 py-2 text-text-secondary bg-white border border-border rounded-md hover:bg-[#f8fafe] transition-colors text-sm font-semibold w-full sm:w-auto justify-center">
            <Filter size={16} />
            Bộ lọc
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-text-secondary">
            <thead className="bg-[#f8fafe] text-text-muted border-b border-border">
              <tr>
                <th className="px-6 py-4 font-semibold">Mã đề nghị</th>
                <th className="px-6 py-4 font-semibold">Tiêu đề</th>
                <th className="px-6 py-4 font-semibold">Số tiền</th>
                <th className="px-6 py-4 font-semibold">AI risk</th>
                <th className="px-6 py-4 font-semibold">Trạng thái</th>
                <th className="px-6 py-4 font-semibold text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-text-muted">Đang tải đề nghị...</td>
                </tr>
              ) : requests.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-text-muted">Chưa có đề nghị phù hợp.</td>
                </tr>
              ) : (
                requests.map((request) => (
                  <tr key={request.id} className="hover:bg-[#f8fafe] transition-colors">
                    <td className="px-6 py-4 font-mono text-xs text-text-muted">{request.requestNumber}</td>
                    <td className="px-6 py-4">
                      <p className="font-semibold text-text-primary">{request.title}</p>
                      <p className="text-xs text-text-muted mt-1">{request.items.length} dòng chi tiết</p>
                    </td>
                    <td className="px-6 py-4 font-semibold text-text-primary">{currency.format(request.totalAmount)}</td>
                    <td className="px-6 py-4">
                      {request.aiRiskScore == null ? (
                        <span className="text-xs text-text-muted">Chưa phân tích</span>
                      ) : (
                        <span className={cn(
                          "inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold",
                          request.aiRiskScore >= 70 ? "bg-red-50 text-red-700" : request.aiRiskScore >= 40 ? "bg-amber-50 text-amber-700" : "bg-emerald-50 text-emerald-700"
                        )}>
                          <AlertTriangle size={12} />
                          {request.aiRiskScore.toFixed(0)}/100
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <span className={cn(
                        "inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold border",
                        request.status === "Approved" && "bg-emerald-50 text-emerald-700 border-emerald-200",
                        request.status === "PendingApproval" && "bg-amber-50 text-amber-700 border-amber-200",
                        request.status === "Rejected" && "bg-red-50 text-red-700 border-red-200",
                        request.status === "Draft" && "bg-slate-50 text-slate-700 border-slate-200"
                      )}>
                        {request.status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      {request.status === "Draft" ? (
                        <button
                          onClick={() => submitMutation.mutate(request.id)}
                          disabled={submitMutation.isPending}
                          className="inline-flex items-center gap-2 px-3 py-2 text-sm font-semibold text-white bg-primary rounded-md hover:bg-primary-dark disabled:opacity-60"
                        >
                          <Send size={14} />
                          Gửi duyệt
                        </button>
                      ) : (
                        <span className="text-xs text-text-muted">Đang theo dõi</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
