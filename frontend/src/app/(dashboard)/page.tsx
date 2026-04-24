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
        <div className="h-10 w-48 bg-slate-200 animate-pulse rounded-md"></div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="h-32 bg-white rounded-xl border border-slate-100 animate-pulse"></div>)}
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

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard 
          title="Total Income" 
          value={currency.format(dashboard?.totalIncome ?? 0)} 
          icon={<DollarSign className="text-emerald-500" size={20} />} 
          trend={{ value: 12.5, isPositive: true }}
        />
        <StatCard 
          title="Total Expenses" 
          value={currency.format(dashboard?.totalExpense ?? 0)} 
          icon={<PieChart className="text-rose-500" size={20} />} 
          trend={{ value: 4.2, isPositive: false }}
        />
        <StatCard 
          title="Net Cashflow" 
          value={currency.format(netCash)} 
          icon={<Activity className="text-blue-500" size={20} />} 
        />
        <StatCard 
          title="Pending Approvals" 
          value={dashboard?.pendingApprovals ?? 0} 
          icon={<Clock className="text-amber-500" size={20} />} 
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="p-5 border-b border-slate-100 flex items-center justify-between">
              <h2 className="text-lg font-semibold text-slate-900">Recent Payment Requests</h2>
              <button className="text-sm font-medium text-blue-600 hover:text-blue-700">View All</button>
            </div>
            <div className="divide-y divide-slate-100">
              {queue?.items?.length === 0 ? (
                <div className="p-8 text-center text-slate-500">No pending approvals.</div>
              ) : (
                queue?.items?.slice(0, 5).map(item => (
                  <div key={item.instanceId} className="p-4 flex items-center justify-between hover:bg-slate-50 transition-colors">
                    <div>
                      <p className="font-medium text-slate-900">{item.entityType}</p>
                      <p className="text-sm text-slate-500">ID: {item.entityId}</p>
                    </div>
                    <div className="text-right">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800">
                        {item.status}
                      </span>
                      <p className="text-xs text-slate-500 mt-1">Step {item.currentStepOrder}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        <div className="space-y-6">
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="p-5 border-b border-slate-100">
              <h2 className="text-lg font-semibold text-slate-900 flex items-center gap-2">
                <AlertCircle className="text-amber-500" size={20} />
                Risk Alerts
              </h2>
            </div>
            <div className="p-5">
              <ul className="space-y-4">
                {dashboard?.riskAlerts?.length ? (
                  dashboard.riskAlerts.map((alert, i) => (
                    <li key={i} className="flex gap-3 text-sm">
                      <span className="shrink-0 w-1.5 h-1.5 rounded-full bg-red-500 mt-1.5"></span>
                      <span className="text-slate-700">{alert}</span>
                    </li>
                  ))
                ) : (
                  <li className="text-sm text-slate-500">No active risk alerts.</li>
                )}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
