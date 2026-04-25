"use client";

import { useMemo, useState } from "react";
import { Filter, Plus, Search, Target } from "lucide-react";
import { PageHeader } from "@/components/ui/PageHeader";
import { useKeyResults, useObjectives, usePeriods } from "@/lib/api/hooks";
import { cn } from "@/lib/utils";

export default function ObjectivesPage() {
  const [search, setSearch] = useState("");
  const { data: objectives, isLoading } = useObjectives(1, 100, search);
  const { data: keyResults } = useKeyResults(undefined, 1, 300);
  const { data: periods } = usePeriods(1, 100);

  const krByObjective = useMemo(() => {
    const map = new Map<string, number>();
    (keyResults?.items ?? []).forEach((kr) => map.set(kr.objectiveId, (map.get(kr.objectiveId) ?? 0) + 1));
    return map;
  }, [keyResults?.items]);

  const periodName = (id: string) => periods?.items?.find((period) => period.id === id)?.name ?? "Không rõ kỳ";

  return (
    <div className="space-y-6">
      <PageHeader
        title="OKR"
        description="Theo dõi mục tiêu công ty, phòng ban, cá nhân và tiến độ Key Result."
      >
        <button className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary-dark transition-colors font-semibold text-sm">
          <Plus size={16} />
          Objective mới
        </button>
      </PageHeader>

      <div className="bg-white rounded-lg border border-border shadow-sm overflow-hidden">
        <div className="p-4 border-b border-border flex flex-col sm:flex-row gap-4 justify-between items-center bg-[#f8fafe]">
          <div className="relative w-full sm:w-80">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm objective"
              className="w-full h-10 pl-9 pr-4 rounded-md border border-border text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            />
          </div>
          <button className="inline-flex items-center gap-2 px-3 py-2 text-text-secondary bg-white border border-border rounded-md hover:bg-[#f8fafe] text-sm font-semibold w-full sm:w-auto justify-center">
            <Filter size={16} />
            Bộ lọc
          </button>
        </div>

        <div className="divide-y divide-border">
          {isLoading ? (
            <div className="p-8 text-center text-text-muted">Đang tải OKR...</div>
          ) : objectives?.items?.length === 0 ? (
            <div className="p-8 text-center text-text-muted">Chưa có objective phù hợp.</div>
          ) : (
            objectives?.items?.map((objective) => (
              <article key={objective.id} className="p-5 hover:bg-[#f8fafe] transition-colors">
                <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
                  <div className="flex items-start gap-3 min-w-0">
                    <div className="w-10 h-10 rounded-md bg-primary-light text-primary flex items-center justify-center shrink-0">
                      <Target size={20} />
                    </div>
                    <div className="min-w-0">
                      <h2 className="font-bold text-text-primary">{objective.title}</h2>
                      <p className="text-sm text-text-muted mt-1">
                        {periodName(objective.periodId)} · {objective.ownerType} · {krByObjective.get(objective.id) ?? 0} Key Results
                      </p>
                    </div>
                  </div>
                  <div className="w-full lg:w-80">
                    <div className="flex justify-between text-xs font-semibold mb-2">
                      <span className="text-text-muted">Tiến độ</span>
                      <span className="text-text-primary">{objective.progress.toFixed(0)}%</span>
                    </div>
                    <div className="h-2 rounded-full bg-slate-100 overflow-hidden">
                      <div
                        className={cn("h-full rounded-full", objective.progress >= 80 ? "bg-emerald-500" : objective.progress >= 50 ? "bg-primary" : "bg-amber-500")}
                        style={{ width: `${Math.min(objective.progress, 100)}%` }}
                      />
                    </div>
                  </div>
                  <span className="text-xs font-bold px-2.5 py-1 rounded-md bg-slate-50 text-slate-700 border border-slate-200 self-start lg:self-auto">
                    {objective.status}
                  </span>
                </div>
              </article>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
