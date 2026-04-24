"use client";

import { useDashboardOverview, useBudgets, useApprovalQueue } from "@/lib/api/hooks";
import { PageHeader } from "@/components/ui/PageHeader";
import { StatCard } from "@/components/ui/StatCard";
import { DollarSign, PieChart, Activity, AlertCircle, Clock } from "lucide-react";
import { useAuthStore } from "@/stores/auth";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });

export default function DashboardPage() {
  const { data: dashboard, isLoading } = useDashboardOverview();
  const { data: budgets } = useBudgets(1, 5);
  const { data: queue } = useApprovalQueue(1, 5);
  const { user } = useAuthStore();

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-10 w-48 bg-[#e1e8f0] animate-pulse rounded-md"></div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="h-32 bg-white rounded-[14px] border border-border animate-pulse"></div>)}
        </div>
      </div>
    );
  }

  const netCash = (dashboard?.totalIncome ?? 0) - (dashboard?.totalExpense ?? 0);

  return (
    <div className="space-y-6">
      <PageHeader 
        title={`Welcome back, ${user?.fullName?.split(' ')[0] || 'User'}`} 
        description="Here is what's happening with your business today."
      />

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        <StatCard 
          title="Total Income" 
          value={currency.format(dashboard?.totalIncome ?? 0)} 
          icon={<DollarSign size={20} />} 
          trend={{ value: 12.5, isPositive: true }}
        />
        <StatCard 
          title="Total Expenses" 
          value={currency.format(dashboard?.totalExpense ?? 0)} 
          icon={<PieChart size={20} />} 
          trend={{ value: 4.2, isPositive: false }}
        />
        <StatCard 
          title="Net Cashflow" 
          value={currency.format(netCash)} 
          icon={<Activity size={20} />} 
        />
        <StatCard 
          title="Pending Approvals" 
          value={dashboard?.pendingApprovals ?? 0} 
          icon={<Clock size={20} />} 
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white rounded-[14px] border border-border shadow-[0_1px_4px_rgba(26,115,232,0.06),0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
            <div className="p-5 border-b border-border flex items-center justify-between bg-gradient-to-b from-white to-[#f8fafe]">
              <h2 className="text-[1.05rem] font-bold text-text-primary">Recent Payment Requests</h2>
              <button className="text-[0.85rem] font-semibold text-primary hover:text-primary-dark transition-colors">View All</button>
            </div>
            <div className="divide-y divide-border">
              {queue?.items?.length === 0 ? (
                <div className="p-8 text-center text-text-muted text-[0.95rem]">No pending approvals.</div>
              ) : (
                queue?.items?.slice(0, 5).map(item => (
                  <div key={item.instanceId} className="p-4 flex items-center justify-between hover:bg-[#f8fafe] transition-colors cursor-pointer group">
                    <div>
                      <p className="font-semibold text-text-primary text-[0.95rem] group-hover:text-primary transition-colors">{item.entityType}</p>
                      <p className="text-[0.85rem] text-text-muted mt-0.5">ID: {item.entityId}</p>
                    </div>
                    <div className="text-right">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-md text-[0.75rem] font-bold bg-warning-bg text-warning shadow-sm">
                        {item.status}
                      </span>
                      <p className="text-[0.75rem] text-text-muted mt-1.5 font-medium">Step {item.currentStepOrder}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        <div className="space-y-6">
          <div className="bg-white rounded-[14px] border border-border shadow-[0_1px_4px_rgba(26,115,232,0.06),0_1px_2px_rgba(0,0,0,0.04)] overflow-hidden">
            <div className="p-5 border-b border-border bg-gradient-to-b from-white to-[#f8fafe]">
              <h2 className="text-[1.05rem] font-bold text-text-primary flex items-center gap-2">
                <AlertCircle className="text-warning" size={18} />
                Risk Alerts
              </h2>
            </div>
            <div className="p-5">
              <ul className="space-y-4">
                {dashboard?.riskAlerts?.length ? (
                  dashboard.riskAlerts.map((alert, i) => (
                    <li key={i} className="flex gap-3 text-[0.95rem] items-start">
                      <span className="shrink-0 w-2 h-2 rounded-full bg-danger mt-[6px] shadow-[0_0_0_2px_rgba(217,48,37,0.15)]"></span>
                      <span className="text-text-secondary font-medium">{alert}</span>
                    </li>
                  ))
                ) : (
                  <li className="text-[0.95rem] text-text-muted text-center py-4">No active risk alerts.</li>
                )}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
