"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { askAi, createPaymentRequest, getDashboard, login } from "@/lib/api";
import type { DashboardOverview, LoginResponse } from "@/types/api";

const currency = new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });

export default function HomePage() {
  const [auth, setAuth] = useState<LoginResponse | null>(null);
  const [dashboard, setDashboard] = useState<DashboardOverview | null>(null);
  const [email, setEmail] = useState("director@omnibiz.ai");
  const [password, setPassword] = useState("Test@123456");
  const [message, setMessage] = useState("");
  const [aiAnswer, setAiAnswer] = useState("");
  const [status, setStatus] = useState("");
  const [paymentAmount, setPaymentAmount] = useState(15000000);

  const token = auth?.accessToken;
  const canUseWorkspace = Boolean(token);

  useEffect(() => {
    const stored = localStorage.getItem("omnibiz-auth");
    if (stored) {
      setAuth(JSON.parse(stored) as LoginResponse);
    }
  }, []);

  useEffect(() => {
    if (!token) return;
    getDashboard(token).then(setDashboard).catch((error: Error) => setStatus(error.message));
  }, [token]);

  const netCash = useMemo(() => {
    if (!dashboard) return 0;
    return dashboard.totalIncome - dashboard.totalExpense;
  }, [dashboard]);

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Dang dang nhap...");
    try {
      const result = await login(email, password);
      setAuth(result);
      localStorage.setItem("omnibiz-auth", JSON.stringify(result));
      setStatus("Dang nhap thanh cong");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Dang nhap that bai");
    }
  }

  async function handleAskAi(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token || !message.trim()) return;
    setStatus("AI dang phan tich...");
    try {
      const result = await askAi(token, message);
      setAiAnswer(result.content);
      setStatus("AI da tra loi");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "AI request failed");
    }
  }

  async function handleCreatePaymentRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!token || !auth?.user.departmentId) {
      setStatus("Tai khoan nay chua co departmentId trong token.");
      return;
    }

    setStatus("Dang tao de nghi chi...");
    try {
      await createPaymentRequest(token, {
        title: "De nghi thanh toan demo tu frontend",
        description: "Khoan chi duoc tao tu OmniBiz AI web client",
        departmentId: auth.user.departmentId,
        requesterId: auth.user.id,
        categoryId: "00000000-0000-0000-0000-000000000000",
        currency: "VND",
        priority: "Normal",
        items: [{ description: "Demo service", quantity: 1, unit: "lot", unitPrice: paymentAmount, totalPrice: paymentAmount }]
      });
      setStatus("Da tao de nghi chi. Hay thay categoryId/requesterId bang du lieu seed thuc te khi demo flow.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Tao de nghi chi that bai");
    }
  }

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">OmniBiz AI</div>
        <nav>
          <a>Dashboard</a>
          <a>Finance</a>
          <a>KPI/OKR</a>
          <a>Workflow</a>
          <a>AI Copilot</a>
        </nav>
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">SME Operating System</p>
            <h1>Dashboard dieu hanh</h1>
          </div>
          <span className="status">{status || "San sang"}</span>
        </header>

        {!canUseWorkspace ? (
          <form className="panel login" onSubmit={handleLogin}>
            <h2>Dang nhap</h2>
            <label>Email<input value={email} onChange={(event) => setEmail(event.target.value)} /></label>
            <label>Mat khau<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} /></label>
            <button type="submit">Dang nhap</button>
          </form>
        ) : (
          <>
            <section className="metrics">
              <Metric title="Tong thu" value={currency.format(dashboard?.totalIncome ?? 0)} />
              <Metric title="Tong chi" value={currency.format(dashboard?.totalExpense ?? 0)} />
              <Metric title="Dong tien rong" value={currency.format(netCash)} />
              <Metric title="KPI trung binh" value={`${dashboard?.averageKpiProgress ?? 0}%`} />
              <Metric title="Cho duyet" value={`${dashboard?.pendingApprovals ?? 0}`} />
            </section>

            <section className="grid">
              <div className="panel">
                <h2>Canh bao rui ro</h2>
                <ul className="risk-list">
                  {(dashboard?.riskAlerts.length ? dashboard.riskAlerts : ["Chua co canh bao moi"]).map((risk) => <li key={risk}>{risk}</li>)}
                </ul>
              </div>

              <form className="panel" onSubmit={handleAskAi}>
                <h2>AI Copilot</h2>
                <textarea value={message} onChange={(event) => setMessage(event.target.value)} placeholder="Hoi: Phong nao vuot ngan sach?" />
                <button type="submit">Gui cau hoi</button>
                {aiAnswer && <pre>{aiAnswer}</pre>}
              </form>

              <form className="panel" onSubmit={handleCreatePaymentRequest}>
                <h2>Quick payment request</h2>
                <label>So tien<input type="number" value={paymentAmount} onChange={(event) => setPaymentAmount(Number(event.target.value))} /></label>
                <button type="submit">Tao de nghi chi</button>
              </form>
            </section>
          </>
        )}
      </section>
    </main>
  );
}

function Metric({ title, value }: { title: string; value: string }) {
  return (
    <div className="metric">
      <span>{title}</span>
      <strong>{value}</strong>
    </div>
  );
}
