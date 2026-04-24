"use client";

import { useBudgets } from "@/lib/api/hooks";
import { PageHeader } from "@/components/ui/PageHeader";
import { Plus, Search, Filter } from "lucide-react";
import { cn } from "@/lib/utils";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });

export default function BudgetsPage() {
  const { data, isLoading } = useBudgets(1, 20);

  return (
    <div className="space-y-6">
      <PageHeader 
        title="Budgets" 
        description="Manage your department budgets and track utilization."
      >
        <button className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
          <Plus size={16} />
          New Budget
        </button>
      </PageHeader>

      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-4 border-b border-slate-100 flex flex-col sm:flex-row gap-4 justify-between items-center bg-slate-50/50">
          <div className="relative w-full sm:w-72">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <input 
              type="text" 
              placeholder="Search budgets..." 
              className="w-full h-10 pl-9 pr-4 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
            />
          </div>
          <button className="flex items-center gap-2 px-3 py-2 text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors text-sm font-medium w-full sm:w-auto justify-center">
            <Filter size={16} />
            Filters
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 text-slate-500 font-medium border-b border-slate-100">
              <tr>
                <th className="px-6 py-4 font-medium">Budget Name</th>
                <th className="px-6 py-4 font-medium">Allocated</th>
                <th className="px-6 py-4 font-medium">Spent</th>
                <th className="px-6 py-4 font-medium">Remaining</th>
                <th className="px-6 py-4 font-medium">Utilization</th>
                <th className="px-6 py-4 font-medium text-right">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-slate-500">Loading budgets...</td>
                </tr>
              ) : data?.items?.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-slate-500">No budgets found.</td>
                </tr>
              ) : (
                data?.items?.map(budget => (
                  <tr key={budget.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4 font-medium text-slate-900">{budget.name}</td>
                    <td className="px-6 py-4">{currency.format(budget.allocatedAmount)}</td>
                    <td className="px-6 py-4">{currency.format(budget.spentAmount)}</td>
                    <td className="px-6 py-4 font-medium">{currency.format(budget.remainingAmount)}</td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-full bg-slate-100 rounded-full h-2 max-w-[100px]">
                          <div 
                            className={cn(
                              "h-2 rounded-full",
                              budget.utilizationPercent > 90 ? "bg-red-500" : budget.utilizationPercent > 75 ? "bg-amber-500" : "bg-emerald-500"
                            )} 
                            style={{ width: `${Math.min(budget.utilizationPercent, 100)}%` }}
                          ></div>
                        </div>
                        <span className="text-xs text-slate-500 w-8">{budget.utilizationPercent.toFixed(0)}%</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <span className={cn(
                        "inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium border",
                        budget.status === 'Active' ? "bg-emerald-50 text-emerald-700 border-emerald-200" : "bg-slate-100 text-slate-700 border-slate-200"
                      )}>
                        {budget.status}
                      </span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        
        {data && data.totalPages > 1 && (
          <div className="p-4 border-t border-slate-100 flex items-center justify-between text-sm text-slate-500">
            <span>Showing {data.items.length} of {data.totalItems} results</span>
            <div className="flex gap-2">
              <button disabled className="px-3 py-1 border border-slate-200 rounded disabled:opacity-50">Prev</button>
              <button className="px-3 py-1 border border-slate-200 rounded">Next</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
