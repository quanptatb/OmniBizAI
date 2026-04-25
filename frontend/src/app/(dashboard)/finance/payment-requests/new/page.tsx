"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle, Plus, Save, Send, Trash2 } from "lucide-react";
import { PageHeader } from "@/components/ui/PageHeader";
import { useAuthStore } from "@/stores/auth";
import { useBudgets, useCategories, useCreatePaymentRequest, useDepartments, useVendors } from "@/lib/api/hooks";

const emptyItem = { description: "", quantity: 1, unit: "Item", unitPrice: 0, totalPrice: 0 };

export default function NewPaymentRequestPage() {
  const router = useRouter();
  const { user } = useAuthStore();
  const createMutation = useCreatePaymentRequest();
  const { data: departments } = useDepartments(1, 100);
  const { data: categories } = useCategories(1, 100);
  const { data: vendors } = useVendors(1, 100);
  const { data: budgets } = useBudgets(1, 100);
  const expenseCategories = useMemo(() => (categories?.items ?? []).filter((category) => category.type === "Expense"), [categories?.items]);

  const [formData, setFormData] = useState({
    title: "",
    description: "",
    departmentId: "",
    categoryId: "",
    vendorId: "",
    budgetId: "",
    currency: "VND",
    paymentMethod: "BankTransfer",
    paymentDueDate: "",
    priority: "Normal",
    items: [emptyItem],
  });

  useEffect(() => {
    setFormData((prev) => ({
      ...prev,
      departmentId: prev.departmentId || departments?.items?.[0]?.id || "",
      categoryId: prev.categoryId || expenseCategories?.[0]?.id || "",
      vendorId: prev.vendorId || vendors?.items?.[0]?.id || "",
    }));
  }, [departments?.items, expenseCategories, vendors?.items]);

  const totalAmount = formData.items.reduce((sum, item) => sum + item.totalPrice, 0);
  const matchedBudget = budgets?.items?.find((budget) => budget.id === formData.budgetId)
    ?? budgets?.items?.find((budget) => budget.departmentId === formData.departmentId && budget.categoryId === formData.categoryId);
  const projectedUtilization = matchedBudget && matchedBudget.allocatedAmount > 0
    ? ((matchedBudget.spentAmount + totalAmount) / matchedBudget.allocatedAmount) * 100
    : 0;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!user?.id || !formData.departmentId || !formData.categoryId || totalAmount <= 0) return;

    createMutation.mutate({
      ...formData,
      requesterId: user.id,
      vendorId: formData.vendorId || undefined,
      budgetId: formData.budgetId || matchedBudget?.id,
      paymentDueDate: formData.paymentDueDate || undefined,
      items: formData.items.map((item) => ({ ...item, quantity: Number(item.quantity), unitPrice: Number(item.unitPrice), totalPrice: Number(item.totalPrice) })),
    }, {
      onSuccess: () => router.push("/finance/payment-requests"),
    });
  };

  const addItem = () => {
    setFormData((prev) => ({ ...prev, items: [...prev.items, { ...emptyItem }] }));
  };

  const removeItem = (index: number) => {
    setFormData((prev) => ({ ...prev, items: prev.items.filter((_, itemIndex) => itemIndex !== index) }));
  };

  const updateItem = (index: number, field: "description" | "quantity" | "unit" | "unitPrice", value: string | number) => {
    const items = [...formData.items];
    const next = { ...items[index], [field]: value };
    next.totalPrice = Number(next.quantity) * Number(next.unitPrice);
    items[index] = next;
    setFormData((prev) => ({ ...prev, items }));
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      <PageHeader
        title="Tạo đề nghị thanh toán"
        description="Nhập khoản chi, dòng chi tiết và gửi vào workflow duyệt."
      />

      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-border shadow-sm overflow-hidden">
        <div className="p-6 space-y-7">
          <section className="space-y-4">
            <h2 className="text-lg font-bold text-text-primary border-b border-border pb-2">Thông tin chung</h2>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <label className="md:col-span-2 text-sm font-semibold text-text-secondary">
                Tiêu đề
                <input
                  required
                  value={formData.title}
                  onChange={(event) => setFormData({ ...formData, title: event.target.value })}
                  className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                  placeholder="VD: Chiến dịch quảng cáo quý 2"
                />
              </label>
              <label className="md:col-span-2 text-sm font-semibold text-text-secondary">
                Mô tả
                <textarea
                  rows={3}
                  value={formData.description}
                  onChange={(event) => setFormData({ ...formData, description: event.target.value })}
                  className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
                  placeholder="Bối cảnh, mục tiêu và tài liệu tham chiếu"
                />
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Phòng ban
                <select value={formData.departmentId} onChange={(event) => setFormData({ ...formData, departmentId: event.target.value, budgetId: "" })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm">
                  {(departments?.items ?? []).map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
                </select>
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Danh mục chi
                <select value={formData.categoryId} onChange={(event) => setFormData({ ...formData, categoryId: event.target.value, budgetId: "" })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm">
                  {expenseCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Nhà cung cấp
                <select value={formData.vendorId} onChange={(event) => setFormData({ ...formData, vendorId: event.target.value })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm">
                  <option value="">Không chọn</option>
                  {(vendors?.items ?? []).map((vendor) => <option key={vendor.id} value={vendor.id}>{vendor.name}</option>)}
                </select>
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Ngân sách
                <select value={formData.budgetId} onChange={(event) => setFormData({ ...formData, budgetId: event.target.value })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm">
                  <option value="">Tự khớp theo phòng ban/danh mục</option>
                  {(budgets?.items ?? []).map((budget) => <option key={budget.id} value={budget.id}>{budget.name}</option>)}
                </select>
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Hạn thanh toán
                <input type="date" value={formData.paymentDueDate} onChange={(event) => setFormData({ ...formData, paymentDueDate: event.target.value })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm" />
              </label>
              <label className="text-sm font-semibold text-text-secondary">
                Mức ưu tiên
                <select value={formData.priority} onChange={(event) => setFormData({ ...formData, priority: event.target.value })} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm">
                  <option value="Low">Low</option>
                  <option value="Normal">Normal</option>
                  <option value="High">High</option>
                  <option value="Urgent">Urgent</option>
                </select>
              </label>
            </div>
          </section>

          <section className="space-y-4">
            <div className="flex justify-between items-center border-b border-border pb-2">
              <h2 className="text-lg font-bold text-text-primary">Chi tiết khoản chi</h2>
              <button type="button" onClick={addItem} className="inline-flex items-center gap-2 text-sm text-primary hover:text-primary-dark font-semibold">
                <Plus size={16} />
                Thêm dòng
              </button>
            </div>

            <div className="space-y-3">
              {formData.items.map((item, index) => (
                <div key={index} className="grid grid-cols-12 gap-3 items-end border border-border rounded-lg p-3">
                  <label className="col-span-12 md:col-span-5 text-xs font-semibold text-text-muted">
                    Mô tả
                    <input required value={item.description} onChange={(event) => updateItem(index, "description", event.target.value)} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm text-text-primary" />
                  </label>
                  <label className="col-span-4 md:col-span-2 text-xs font-semibold text-text-muted">
                    SL
                    <input required type="number" min="1" value={item.quantity} onChange={(event) => updateItem(index, "quantity", event.target.value)} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm text-text-primary" />
                  </label>
                  <label className="col-span-8 md:col-span-3 text-xs font-semibold text-text-muted">
                    Đơn giá
                    <input required type="number" min="0" value={item.unitPrice} onChange={(event) => updateItem(index, "unitPrice", event.target.value)} className="mt-1 w-full rounded-md border border-border px-3 py-2 text-sm text-text-primary" />
                  </label>
                  <div className="col-span-10 md:col-span-1">
                    <p className="text-xs font-semibold text-text-muted mb-1">Thành tiền</p>
                    <p className="h-10 flex items-center text-sm font-bold text-text-primary">{new Intl.NumberFormat("vi-VN").format(item.totalPrice)}</p>
                  </div>
                  <button type="button" onClick={() => removeItem(index)} disabled={formData.items.length === 1} className="col-span-2 md:col-span-1 h-10 inline-flex items-center justify-center text-red-600 hover:bg-red-50 rounded-md disabled:opacity-30">
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>
          </section>

          <section className="rounded-lg border border-border bg-[#f8fafe] p-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-sm font-semibold text-text-primary">AI Spend Guardrail</p>
              <p className="text-sm text-text-muted mt-1">
                {matchedBudget
                  ? `Sau khoản chi này, ngân sách "${matchedBudget.name}" dự kiến đạt ${projectedUtilization.toFixed(1)}%.`
                  : "Hệ thống sẽ tự phân tích ngân sách và lịch sử NCC khi gửi duyệt."}
              </p>
            </div>
            <div className="inline-flex items-center gap-2 text-sm font-bold text-amber-700 bg-amber-50 border border-amber-200 rounded-md px-3 py-2">
              <AlertTriangle size={16} />
              {projectedUtilization >= 100 ? "Rủi ro cao" : projectedUtilization >= 80 ? "Cần lưu ý" : "Bình thường"}
            </div>
          </section>
        </div>

        <div className="p-4 border-t border-border bg-[#f8fafe] flex flex-col sm:flex-row justify-end gap-3">
          <button type="button" onClick={() => router.back()} className="px-4 py-2 text-sm font-semibold text-text-secondary bg-white border border-border rounded-md hover:bg-slate-50">
            Hủy
          </button>
          <button type="submit" className="inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold text-text-secondary bg-white border border-border rounded-md hover:bg-slate-50">
            <Save size={16} />
            Lưu nháp
          </button>
          <button type="submit" disabled={createMutation.isPending || !user?.id || !formData.departmentId || !formData.categoryId || totalAmount <= 0} className="inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-primary rounded-md hover:bg-primary-dark disabled:opacity-60">
            <Send size={16} />
            {createMutation.isPending ? "Đang lưu..." : "Gửi vào hệ thống"}
          </button>
        </div>
      </form>
    </div>
  );
}
