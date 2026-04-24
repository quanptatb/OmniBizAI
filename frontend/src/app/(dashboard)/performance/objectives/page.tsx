"use client";

import { PageHeader } from "@/components/ui/PageHeader";
import { Plus, Search, Filter } from "lucide-react";

export default function ObjectivesPage() {
  return (
    <div className="space-y-6">
      <PageHeader 
        title="Objectives (OKR)" 
        description="Track company and departmental objectives and key results."
      >
        <button className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium text-sm">
          <Plus size={16} />
          New Objective
        </button>
      </PageHeader>

      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-4 border-b border-slate-100 flex flex-col sm:flex-row gap-4 justify-between items-center bg-slate-50/50">
          <div className="relative w-full sm:w-72">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <input 
              type="text" 
              placeholder="Search objectives..." 
              className="w-full h-10 pl-9 pr-4 rounded-lg border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all"
            />
          </div>
          <button className="flex items-center gap-2 px-3 py-2 text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors text-sm font-medium w-full sm:w-auto justify-center">
            <Filter size={16} />
            Filters
          </button>
        </div>

        <div className="p-12 text-center text-slate-500">
          <p>Please implement the OKR tree view here.</p>
        </div>
      </div>
    </div>
  );
}
