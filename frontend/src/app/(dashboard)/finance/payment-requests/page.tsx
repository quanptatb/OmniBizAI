"use client";

import { useState } from "react";
import { PageHeader } from "@/components/ui/PageHeader";
import { Plus, Search, Filter } from "lucide-react";
import Link from "next/link";

export default function PaymentRequestsPage() {
  const [activeTab, setActiveTab] = useState("all");

  const tabs = [
    { id: "all", label: "All Requests" },
    { id: "draft", label: "Draft" },
    { id: "pending", label: "Pending Approval" },
    { id: "approved", label: "Approved" },
    { id: "rejected", label: "Rejected" },
  ];

  return (
    <div className="space-y-6">
      <PageHeader 
        title="Payment Requests" 
        description="Manage and track payment requests across the organization."
      >
        <Link href="/finance/payment-requests/new" className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
          <Plus size={16} />
          New Request
        </Link>
      </PageHeader>

      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="border-b border-slate-100">
          <nav className="flex gap-6 px-6" aria-label="Tabs">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`py-4 px-1 inline-flex items-center border-b-2 font-medium text-sm transition-colors ${
                  activeTab === tab.id
                    ? "border-blue-600 text-blue-600"
                    : "border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300"
                }`}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        <div className="p-4 border-b border-slate-100 flex flex-col sm:flex-row gap-4 justify-between items-center bg-slate-50/50">
          <div className="relative w-full sm:w-72">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <input 
              type="text" 
              placeholder="Search requests..." 
              className="w-full h-10 pl-9 pr-4 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
            />
          </div>
          <button className="flex items-center gap-2 px-3 py-2 text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors text-sm font-medium w-full sm:w-auto justify-center">
            <Filter size={16} />
            Filters
          </button>
        </div>

        <div className="p-12 text-center text-slate-500">
          <p>Please implement the table with actual data here.</p>
        </div>
      </div>
    </div>
  );
}
