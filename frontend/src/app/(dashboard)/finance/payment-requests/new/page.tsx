"use client";

import { useState } from "react";
import { PageHeader } from "@/components/ui/PageHeader";
import { useCreatePaymentRequest } from "@/lib/api/hooks";
import { useRouter } from "next/navigation";
import { Save, Send } from "lucide-react";

export default function NewPaymentRequestPage() {
  const router = useRouter();
  const createMutation = useCreatePaymentRequest();
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    departmentId: "dept-1",
    categoryId: "cat-1",
    currency: "VND",
    priority: "Normal",
    items: [{ description: "", quantity: 1, unitPrice: 0, totalPrice: 0 }]
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO_BACKEND_MISSING: We are using a mock requesterId here
    createMutation.mutate({
      ...formData,
      requesterId: "req-1"
    }, {
      onSuccess: () => {
        router.push("/finance/payment-requests");
      }
    });
  };

  const addItem = () => {
    setFormData(prev => ({
      ...prev,
      items: [...prev.items, { description: "", quantity: 1, unitPrice: 0, totalPrice: 0 }]
    }));
  };

  const updateItem = (index: number, field: string, value: string | number) => {
    const newItems = [...formData.items];
    newItems[index] = { ...newItems[index], [field]: value };
    if (field === 'quantity' || field === 'unitPrice') {
      newItems[index].totalPrice = Number(newItems[index].quantity) * Number(newItems[index].unitPrice);
    }
    setFormData(prev => ({ ...prev, items: newItems }));
  };

  const totalAmount = formData.items.reduce((sum, item) => sum + item.totalPrice, 0);

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <PageHeader 
        title="New Payment Request" 
        description="Create a new payment request for approval."
      />

      <form onSubmit={handleSubmit} className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-6 space-y-6">
          <div className="space-y-4">
            <h3 className="text-lg font-medium text-slate-900 border-b border-slate-100 pb-2">General Information</h3>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-slate-700 mb-1">Title</label>
                <input required type="text" value={formData.title} onChange={e => setFormData({...formData, title: e.target.value})} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" placeholder="e.g. Q3 Marketing Campaign Ads" />
              </div>
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
                <textarea rows={3} value={formData.description} onChange={e => setFormData({...formData, description: e.target.value})} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500" placeholder="Provide context for this request..."></textarea>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Department</label>
                <select value={formData.departmentId} onChange={e => setFormData({...formData, departmentId: e.target.value})} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500">
                  <option value="dept-1">Marketing</option>
                  <option value="dept-2">Engineering</option>
                  <option value="dept-3">Sales</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Category</label>
                <select value={formData.categoryId} onChange={e => setFormData({...formData, categoryId: e.target.value})} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500">
                  <option value="cat-1">Software Subscriptions</option>
                  <option value="cat-2">Hardware</option>
                  <option value="cat-3">Travel</option>
                </select>
              </div>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex justify-between items-center border-b border-slate-100 pb-2">
              <h3 className="text-lg font-medium text-slate-900">Line Items</h3>
              <button type="button" onClick={addItem} className="text-sm text-blue-600 hover:text-blue-700 font-medium">+ Add Item</button>
            </div>
            
            {formData.items.map((item, index) => (
              <div key={index} className="grid grid-cols-12 gap-3 items-start border-b border-slate-50 pb-4">
                <div className="col-span-5">
                  <label className="block text-xs font-medium text-slate-500 mb-1">Description</label>
                  <input required type="text" value={item.description} onChange={e => updateItem(index, 'description', e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" placeholder="Item description" />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-slate-500 mb-1">Quantity</label>
                  <input required type="number" min="1" value={item.quantity} onChange={e => updateItem(index, 'quantity', e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
                </div>
                <div className="col-span-3">
                  <label className="block text-xs font-medium text-slate-500 mb-1">Unit Price</label>
                  <input required type="number" min="0" value={item.unitPrice} onChange={e => updateItem(index, 'unitPrice', e.target.value)} className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
                </div>
                <div className="col-span-2">
                  <label className="block text-xs font-medium text-slate-500 mb-1">Total</label>
                  <div className="px-3 py-2 text-sm font-medium text-slate-900 bg-slate-50 rounded-md border border-slate-200">
                    {new Intl.NumberFormat('vi-VN').format(item.totalPrice)}
                  </div>
                </div>
              </div>
            ))}
            
            <div className="flex justify-end pt-2">
              <div className="text-right">
                <span className="text-sm text-slate-500 mr-4">Total Amount:</span>
                <span className="text-xl font-bold text-slate-900">{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: formData.currency }).format(totalAmount)}</span>
              </div>
            </div>
          </div>
        </div>
        
        <div className="p-4 border-t border-slate-100 bg-slate-50 flex justify-end gap-3">
          <button type="button" onClick={() => router.back()} className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors">
            Cancel
          </button>
          <button type="button" className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors">
            <Save size={16} /> Save Draft
          </button>
          <button type="submit" disabled={createMutation.isPending} className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50">
            <Send size={16} /> {createMutation.isPending ? "Submitting..." : "Submit Request"}
          </button>
        </div>
      </form>
    </div>
  );
}
