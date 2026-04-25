"use client";

import { useMemo, useState } from "react";
import { Building2, Filter, Plus, Search, Users } from "lucide-react";
import { PageHeader } from "@/components/ui/PageHeader";
import { useDepartments, useEmployees } from "@/lib/api/hooks";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });

export default function DepartmentsPage() {
  const [search, setSearch] = useState("");
  const { data: departments, isLoading } = useDepartments(1, 100, search);
  const { data: employees } = useEmployees(1, 200);

  const employeeCountByDepartment = useMemo(() => {
    const map = new Map<string, number>();
    (employees?.items ?? []).forEach((employee) => {
      if (!employee.departmentId) return;
      map.set(employee.departmentId, (map.get(employee.departmentId) ?? 0) + 1);
    });
    return map;
  }, [employees?.items]);

  const managerName = (id?: string) => employees?.items?.find((employee) => employee.id === id)?.fullName ?? "Chưa phân công";

  return (
    <div className="space-y-6">
      <PageHeader
        title="Tổ chức"
        description="Quản lý phòng ban, ngân sách cấp phòng và số lượng nhân sự."
      >
        <button className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary-dark transition-colors font-semibold text-sm">
          <Plus size={16} />
          Phòng ban mới
        </button>
      </PageHeader>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white border border-border rounded-lg p-5">
          <p className="text-sm text-text-muted font-semibold">Phòng ban</p>
          <p className="text-3xl font-black text-text-primary mt-2">{departments?.totalItems ?? 0}</p>
        </div>
        <div className="bg-white border border-border rounded-lg p-5">
          <p className="text-sm text-text-muted font-semibold">Nhân sự active</p>
          <p className="text-3xl font-black text-text-primary mt-2">{employees?.items?.filter((employee) => employee.status === "Active").length ?? 0}</p>
        </div>
        <div className="bg-white border border-border rounded-lg p-5">
          <p className="text-sm text-text-muted font-semibold">Tổng hạn mức</p>
          <p className="text-3xl font-black text-text-primary mt-2">{currency.format((departments?.items ?? []).reduce((sum, item) => sum + item.budgetLimit, 0))}</p>
        </div>
      </div>

      <div className="bg-white rounded-lg border border-border shadow-sm overflow-hidden">
        <div className="p-4 border-b border-border flex flex-col sm:flex-row gap-4 justify-between items-center bg-[#f8fafe]">
          <div className="relative w-full sm:w-80">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-text-muted" />
            <input
              type="text"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Tìm phòng ban"
              className="w-full h-10 pl-9 pr-4 rounded-md border border-border text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            />
          </div>
          <button className="inline-flex items-center gap-2 px-3 py-2 text-text-secondary bg-white border border-border rounded-md hover:bg-[#f8fafe] text-sm font-semibold w-full sm:w-auto justify-center">
            <Filter size={16} />
            Bộ lọc
          </button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 p-4">
          {isLoading ? (
            <div className="col-span-full p-8 text-center text-text-muted">Đang tải phòng ban...</div>
          ) : departments?.items?.length === 0 ? (
            <div className="col-span-full p-8 text-center text-text-muted">Không có phòng ban phù hợp.</div>
          ) : (
            departments?.items?.map((department) => (
              <article key={department.id} className="border border-border rounded-lg p-4 hover:border-primary/40 transition-colors">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 min-w-0">
                    <div className="w-10 h-10 rounded-md bg-primary-light text-primary flex items-center justify-center shrink-0">
                      <Building2 size={20} />
                    </div>
                    <div className="min-w-0">
                      <h2 className="font-bold text-text-primary truncate">{department.name}</h2>
                      <p className="text-xs text-text-muted mt-1 font-mono">{department.code}</p>
                    </div>
                  </div>
                  <span className="text-xs font-bold px-2.5 py-1 rounded-md bg-emerald-50 text-emerald-700">
                    {department.isActive ? "Active" : "Inactive"}
                  </span>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mt-4 text-sm">
                  <div>
                    <p className="text-text-muted">Quản lý</p>
                    <p className="font-semibold text-text-primary truncate">{managerName(department.managerId)}</p>
                  </div>
                  <div>
                    <p className="text-text-muted">Nhân sự</p>
                    <p className="font-semibold text-text-primary inline-flex items-center gap-1">
                      <Users size={14} />
                      {employeeCountByDepartment.get(department.id) ?? 0}
                    </p>
                  </div>
                  <div>
                    <p className="text-text-muted">Hạn mức</p>
                    <p className="font-semibold text-text-primary">{currency.format(department.budgetLimit)}</p>
                  </div>
                </div>
              </article>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
